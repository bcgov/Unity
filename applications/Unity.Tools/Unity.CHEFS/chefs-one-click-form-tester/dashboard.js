'use strict';

const SVG_NS = 'http://www.w3.org/2000/svg';
let dashboardState = null;
let selectedRun = null;

const elements = {
  contextText: document.getElementById('contextText'),
  runSelect: document.getElementById('runSelect'),
  viewSelect: document.getElementById('viewSelect'),
  chartSelect: document.getElementById('chartSelect'),
  refreshButton: document.getElementById('refreshButton'),
  clearHistoryButton: document.getElementById('clearHistoryButton'),
  resultCard: document.querySelector('.result-card'),
  resultValue: document.getElementById('resultValue'),
  resultDetail: document.getElementById('resultDetail'),
  durationValue: document.getElementById('durationValue'),
  durationDetail: document.getElementById('durationDetail'),
  fieldsValue: document.getElementById('fieldsValue'),
  fieldsDetail: document.getElementById('fieldsDetail'),
  issuesValue: document.getElementById('issuesValue'),
  issuesDetail: document.getElementById('issuesDetail'),
  chartGroupLabel: document.getElementById('chartGroupLabel'),
  chartTitle: document.getElementById('chartTitle'),
  chart: document.getElementById('chart'),
  chartDescription: document.getElementById('chartDescription'),
  chartUnavailable: document.getElementById('chartUnavailable'),
  interpretation: document.getElementById('interpretation'),
  formRefValue: document.getElementById('formRefValue'),
  passesValue: document.getElementById('passesValue'),
  attachmentsValue: document.getElementById('attachmentsValue'),
  rowsValue: document.getElementById('rowsValue'),
  confirmationValue: document.getElementById('confirmationValue'),
  buildValue: document.getElementById('buildValue')
};

function formatDuration(milliseconds) {
  const seconds = Math.max(0, Number(milliseconds) || 0) / 1000;
  if (seconds < 60) return `${seconds.toFixed(seconds < 10 ? 1 : 0)} s`;
  const minutes = Math.floor(seconds / 60);
  const remainder = Math.round(seconds % 60);
  return `${minutes}m ${remainder}s`;
}

function formatNumber(value) {
  return new Intl.NumberFormat('en-CA').format(Math.max(0, Number(value) || 0));
}

function percentile(values, probability) {
  const sorted = values.filter(Number.isFinite).sort((left, right) => left - right);
  if (!sorted.length) return 0;
  const index = (sorted.length - 1) * probability;
  const lower = Math.floor(index);
  const upper = Math.ceil(index);
  return lower === upper
    ? sorted[lower]
    : sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
}

function comparableHistory() {
  if (!selectedRun || !dashboardState) return [];
  return (dashboardState.history || []).filter((run) =>
    run && run.formRef === selectedRun.formRef
  );
}

function allVisibleRuns() {
  const byRef = new Map();
  for (const run of [
    ...(dashboardState && dashboardState.history || []),
    ...(dashboardState && dashboardState.runs || [])
  ]) {
    if (run && run.runRef) byRef.set(run.runRef, run);
  }
  return Array.from(byRef.values());
}

function svgElement(name, attributes, text) {
  const node = document.createElementNS(SVG_NS, name);
  for (const [key, value] of Object.entries(attributes || {})) {
    node.setAttribute(key, String(value));
  }
  if (text !== undefined) node.textContent = String(text);
  return node;
}

function clearChart() {
  while (elements.chart.firstChild) elements.chart.firstChild.remove();
}

function addText(x, y, text, className, anchor) {
  elements.chart.appendChild(svgElement('text', {
    x,
    y,
    class: className || 'chart-label',
    'text-anchor': anchor || 'start'
  }, text));
}

function addLine(x1, y1, x2, y2, className, extra) {
  elements.chart.appendChild(svgElement('line', {
    x1, y1, x2, y2, class: className || 'axis', ...(extra || {})
  }));
}

function addRect(x, y, width, height, className, extra) {
  elements.chart.appendChild(svgElement('rect', {
    x, y, width: Math.max(0, width), height: Math.max(0, height),
    rx: 4, class: className || 'series-primary', ...(extra || {})
  }));
}

function addCircle(cx, cy, r, className, extra) {
  elements.chart.appendChild(svgElement('circle', {
    cx, cy, r, class: className || 'series-primary', ...(extra || {})
  }));
}

function linePath(points) {
  return points.map((point, index) =>
    `${index ? 'L' : 'M'} ${point[0].toFixed(1)} ${point[1].toFixed(1)}`
  ).join(' ');
}

function resultClass(result) {
  if (result === 'submitted' || result === 'completed') return 'success';
  if (result === 'stopped' || result === 'safety_stop') return 'warning';
  return 'failure';
}

function issueTotal(run) {
  const issues = run && run.issueCounts || {};
  return Object.values(issues).reduce((sum, value) => sum + (Number(value) || 0), 0);
}

function plainInterpretation(run) {
  if (!run) return 'No completed result is available.';
  const metrics = run.metrics || {};
  if (run.result === 'submitted' || run.result === 'completed') {
    const issuePhrase = issueTotal(run)
      ? `${formatNumber(issueTotal(run))} diagnostic issue${issueTotal(run) === 1 ? '' : 's'} were recorded.`
      : 'No diagnostic issues were recorded.';
    return `The run completed successfully after ${formatNumber(metrics.passes)} pass${metrics.passes === 1 ? '' : 'es'} and filled ${formatNumber(metrics.filled)} fields. ${issuePhrase}`;
  }
  return `The run ended as ${run.result.replaceAll('_', ' ')}. The failure category is ${run.failureCategory.replaceAll('_', ' ')}, with ${formatNumber(metrics.remaining)} visible fields remaining.`;
}

const chartRegistry = [
  {
    id: 'outcome',
    group: 'simple',
    title: 'Outcome overview',
    description: 'Shows the result mix for the current singleton or batch.',
    availability: () => ({ ok: Boolean(selectedRun), reason: 'Select a completed run.' }),
    render: renderOutcome
  },
  {
    id: 'progress',
    group: 'simple',
    title: 'Field progress',
    description: 'Compares discovered, filled, remaining, failed and unsupported field counts.',
    availability: () => ({ ok: Boolean(selectedRun), reason: 'Select a completed run.' }),
    render: renderProgress
  },
  {
    id: 'durations',
    group: 'simple',
    title: 'Duration by result',
    description: 'Compares total execution time across the current batch.',
    availability: () => ({
      ok: Boolean(dashboardState && dashboardState.runs && dashboardState.runs.length > 1),
      reason: 'This chart needs a batch containing at least two runs.'
    }),
    render: renderDurations
  },
  {
    id: 'phases',
    group: 'simple',
    title: 'Where the time went',
    description: 'Breaks total execution time into initialization, filling, validation and submission.',
    availability: () => ({ ok: Boolean(selectedRun), reason: 'Select a completed run.' }),
    render: renderPhases
  },
  {
    id: 'pass-trend',
    group: 'analyst',
    title: 'Pass-by-pass trend',
    description: 'Tracks filled and remaining fields as the fill loop progresses.',
    availability: () => ({
      ok: Boolean(selectedRun && selectedRun.passSeries && selectedRun.passSeries.length >= 2),
      reason: 'At least two recorded fill passes are required.'
    }),
    render: renderPassTrend
  },
  {
    id: 'strategy-latency',
    group: 'analyst',
    title: 'Strategy latency distribution',
    description: 'Shows minimum, quartiles, median and maximum fill latency by strategy.',
    availability: () => ({
      ok: Boolean(selectedRun && selectedRun.strategies &&
        selectedRun.strategies.some((strategy) => strategy.latencyMs.length >= 2)),
      reason: 'At least one fill strategy needs two measured actions.'
    }),
    render: renderStrategyLatency
  },
  {
    id: 'component-outcomes',
    group: 'analyst',
    title: 'Component outcome flow',
    description: 'Shows how discovered components ended: filled, protected, failed, unsupported or empty.',
    availability: () => ({
      ok: Boolean(selectedRun && Object.keys(selectedRun.componentOutcomes || {}).length),
      reason: 'The run has no component outcome snapshot.'
    }),
    render: renderComponentOutcomes
  },
  {
    id: 'retry-heatmap',
    group: 'analyst',
    title: 'Fill strategy heatmap',
    description: 'Compares attempts, successes and failures across fill strategies.',
    availability: () => ({
      ok: Boolean(selectedRun && selectedRun.strategies && selectedRun.strategies.length),
      reason: 'The run has no fill-strategy evidence.'
    }),
    render: renderStrategyHeatmap
  },
  {
    id: 'duration-histogram',
    group: 'statistical',
    title: 'Duration bell curve',
    description: 'Shows a duration histogram with a normal-distribution reference curve for comparable runs.',
    availability: () => ({
      ok: comparableHistory().length >= 20,
      reason: `Requires ${Math.max(0, 20 - comparableHistory().length)} more retained runs for ${selectedRun ? selectedRun.formRef : 'this form'}.`
    }),
    render: renderHistogram
  },
  {
    id: 'control-chart',
    group: 'statistical',
    title: 'Duration control chart',
    description: 'Tracks comparable run duration against the mean and three-sigma control limits.',
    availability: () => ({
      ok: comparableHistory().length >= 8,
      reason: `Requires ${Math.max(0, 8 - comparableHistory().length)} more retained comparable runs.`
    }),
    render: renderControlChart
  },
  {
    id: 'complexity-scatter',
    group: 'statistical',
    title: 'Complexity versus duration',
    description: 'Plots discovered component count against total duration.',
    availability: () => ({
      ok: allVisibleRuns().length >= 3,
      reason: `Requires ${Math.max(0, 3 - allVisibleRuns().length)} more aggregate runs.`
    }),
    render: renderScatter
  },
  {
    id: 'percentile-bands',
    group: 'statistical',
    title: 'Duration percentiles',
    description: 'Shows the 10th, 25th, 50th, 75th and 90th duration percentiles.',
    availability: () => ({
      ok: comparableHistory().length >= 5,
      reason: `Requires ${Math.max(0, 5 - comparableHistory().length)} more retained comparable runs.`
    }),
    render: renderPercentiles
  },
  {
    id: 'duration-candles',
    group: 'experimental',
    title: 'Duration candlestick time series',
    description: 'Maps each day to first, high, low and final comparable-run duration.',
    availability: () => {
      const dates = new Set(comparableHistory().map((run) => String(run.endedAt || '').slice(0, 10)));
      return {
        ok: comparableHistory().length >= 8 && dates.size >= 2,
        reason: 'Requires at least eight comparable retained runs across two or more days.'
      };
    },
    render: renderCandlesticks
  },
  {
    id: 'event-density',
    group: 'experimental',
    title: 'Pass activity density',
    description: 'Shows recorded actions per pass against elapsed time.',
    availability: () => ({
      ok: Boolean(selectedRun && selectedRun.passSeries && selectedRun.passSeries.length >= 3),
      reason: 'At least three pass checkpoints are required.'
    }),
    render: renderEventDensity
  },
  {
    id: 'build-distribution',
    group: 'experimental',
    title: 'Build duration distributions',
    description: 'Compares duration quartiles between retained extension builds.',
    availability: () => {
      const builds = new Set(allVisibleRuns().map((run) => run.buildNumber).filter(Boolean));
      return {
        ok: allVisibleRuns().length >= 6 && builds.size >= 2,
        reason: 'Requires at least six aggregate runs spanning two extension builds.'
      };
    },
    render: renderBuildDistribution
  }
];

function renderOutcome() {
  const runs = dashboardState.mode === 'batch' ? dashboardState.runs : [selectedRun];
  const counts = {};
  for (const run of runs) counts[run.result] = (counts[run.result] || 0) + 1;
  const total = runs.length;
  const colors = {
    submitted: '#2e8540', completed: '#2e8540', blocked: '#e69f00',
    stalled: '#c62828', failed: '#c62828', stopped: '#7c5fb3', safety_stop: '#7c5fb3'
  };
  let offset = 0;
  const circumference = 2 * Math.PI * 105;
  for (const [result, count] of Object.entries(counts)) {
    const length = circumference * count / total;
    elements.chart.appendChild(svgElement('circle', {
      cx: 260, cy: 215, r: 105, fill: 'none', stroke: colors[result] || '#9fb1c1',
      'stroke-width': 46, 'stroke-dasharray': `${length} ${circumference - length}`,
      'stroke-dashoffset': -offset, transform: 'rotate(-90 260 215)'
    }));
    offset += length;
  }
  addText(
    260,
    210,
    `${(counts.submitted || 0) + (counts.completed || 0)}/${total}`,
    'chart-value',
    'middle'
  );
  addText(260, 234, 'successful', 'chart-label', 'middle');
  let y = 145;
  for (const [result, count] of Object.entries(counts)) {
    addRect(500, y - 13, 18, 18, '', { fill: colors[result] || '#9fb1c1' });
    addText(532, y, result.replaceAll('_', ' '), 'chart-label');
    addText(760, y, count, 'chart-value', 'end');
    y += 42;
  }
}

function renderProgress() {
  const metrics = selectedRun.metrics || {};
  const rows = [
    ['Discovered', metrics.discovered, 'series-muted'],
    ['Filled', metrics.filled, 'series-success'],
    ['Remaining', metrics.remaining, 'series-secondary'],
    ['Failed', metrics.failed, 'series-danger'],
    ['Unsupported', metrics.unsupported, 'series-primary']
  ];
  const maximum = Math.max(1, ...rows.map((row) => row[1]));
  rows.forEach((row, index) => {
    const y = 70 + index * 70;
    addText(30, y + 18, row[0], 'chart-label');
    addRect(170, y, 640 * row[1] / maximum, 28, row[2]);
    addText(830, y + 20, formatNumber(row[1]), 'chart-value', 'end');
  });
}

function renderDurations() {
  const runs = dashboardState.runs || [];
  const maximum = Math.max(1, ...runs.map((run) => run.durationMs));
  const width = Math.max(28, Math.min(82, 700 / runs.length - 12));
  runs.forEach((run, index) => {
    const x = 90 + index * (720 / runs.length);
    const height = 290 * run.durationMs / maximum;
    addRect(x, 350 - height, width, height, `series-${resultClass(run.result)}`);
    addText(x + width / 2, 375, run.batch && run.batch.index || String(index + 1), 'chart-label', 'middle');
    addText(x + width / 2, 340 - height, formatDuration(run.durationMs), 'chart-value', 'middle');
  });
  addLine(70, 350, 850, 350, 'axis');
}

function renderPhases() {
  const phases = selectedRun.phases || {};
  const rows = [
    ['Initialization', phases.initialization, '#5b8ff9'],
    ['Filling', phases.filling, '#2e8540'],
    ['Validation', phases.validation, '#f0a202'],
    ['Submission', phases.submission, '#7c5fb3']
  ];
  const total = Math.max(1, rows.reduce((sum, row) => sum + (row[1] || 0), 0));
  let x = 80;
  rows.forEach((row, index) => {
    const width = 740 * (row[1] || 0) / total;
    addRect(x, 150, width, 72, '', { fill: row[2], rx: 0 });
    if (width > 80) addText(x + width / 2, 192, formatDuration(row[1]), 'chart-value', 'middle');
    addRect(100 + (index % 2) * 350, 290 + Math.floor(index / 2) * 55, 18, 18, '', { fill: row[2] });
    addText(130 + (index % 2) * 350, 304 + Math.floor(index / 2) * 55, row[0], 'chart-label');
    x += width;
  });
}

function renderPassTrend() {
  const series = selectedRun.passSeries;
  const maximum = Math.max(1, ...series.flatMap((item) => [item.filled, item.remaining]));
  const xFor = (index) => 75 + index * 760 / Math.max(1, series.length - 1);
  const yFor = (value) => 360 - value * 290 / maximum;
  [0, 0.25, 0.5, 0.75, 1].forEach((fraction) => {
    const y = 360 - fraction * 290;
    addLine(70, y, 850, y, 'gridline');
    addText(60, y + 4, Math.round(maximum * fraction), 'chart-label', 'end');
  });
  const filled = series.map((item, index) => [xFor(index), yFor(item.filled)]);
  const remaining = series.map((item, index) => [xFor(index), yFor(item.remaining)]);
  elements.chart.appendChild(svgElement('path', {
    d: linePath(filled), fill: 'none', stroke: '#2e8540', 'stroke-width': 4
  }));
  elements.chart.appendChild(svgElement('path', {
    d: linePath(remaining), fill: 'none', stroke: '#e69f00', 'stroke-width': 4
  }));
  filled.forEach((point) => addCircle(point[0], point[1], 4, '', { fill: '#2e8540' }));
  remaining.forEach((point) => addCircle(point[0], point[1], 4, '', { fill: '#e69f00' }));
  addText(720, 45, 'Filled', 'chart-value');
  addLine(680, 41, 710, 41, '', { stroke: '#2e8540', 'stroke-width': 4 });
  addText(815, 45, 'Remaining', 'chart-value');
  addLine(775, 41, 805, 41, '', { stroke: '#e69f00', 'stroke-width': 4 });
}

function renderStrategyLatency() {
  const strategies = selectedRun.strategies.filter((item) => item.latencyMs.length >= 2).slice(0, 7);
  const maximum = Math.max(1, ...strategies.flatMap((item) => item.latencyMs));
  strategies.forEach((strategy, index) => {
    const values = strategy.latencyMs;
    const min = Math.min(...values);
    const max = Math.max(...values);
    const q1 = percentile(values, 0.25);
    const median = percentile(values, 0.5);
    const q3 = percentile(values, 0.75);
    const y = 65 + index * 52;
    const x = (value) => 210 + value * 610 / maximum;
    addText(190, y + 5, strategy.strategy, 'chart-label', 'end');
    addLine(x(min), y, x(max), y, 'axis');
    addRect(x(q1), y - 13, x(q3) - x(q1), 26, 'series-primary');
    addLine(x(median), y - 13, x(median), y + 13, '', { stroke: '#fff', 'stroke-width': 3 });
    addText(840, y + 5, formatDuration(median), 'chart-value', 'end');
  });
}

function renderComponentOutcomes() {
  const entries = Object.entries(selectedRun.componentOutcomes || {})
    .sort((left, right) => right[1] - left[1]);
  const total = Math.max(1, entries.reduce((sum, entry) => sum + entry[1], 0));
  const colors = {
    filled: '#2e8540', protected: '#5b8ff9', failed: '#c62828',
    unsupported: '#7c5fb3', empty: '#e69f00', pending: '#9fb1c1', unknown: '#9fb1c1'
  };
  let x = 80;
  entries.forEach(([status, count], index) => {
    const width = 740 * count / total;
    addRect(x, 125, width, 90, '', { fill: colors[status] || '#9fb1c1', rx: 0 });
    if (width > 55) addText(x + width / 2, 177, count, 'chart-value', 'middle');
    addRect(100 + (index % 3) * 245, 285 + Math.floor(index / 3) * 48, 17, 17, '', {
      fill: colors[status] || '#9fb1c1'
    });
    addText(128 + (index % 3) * 245, 299 + Math.floor(index / 3) * 48, status, 'chart-label');
    x += width;
  });
}

function renderStrategyHeatmap() {
  const strategies = selectedRun.strategies.slice(0, 9);
  const columns = ['attempts', 'successes', 'failures'];
  const maximum = Math.max(1, ...strategies.flatMap((item) => columns.map((column) => item[column])));
  columns.forEach((column, index) => addText(390 + index * 150, 45, column, 'chart-label', 'middle'));
  strategies.forEach((strategy, row) => {
    const y = 65 + row * 38;
    addText(280, y + 23, strategy.strategy, 'chart-label', 'end');
    columns.forEach((column, columnIndex) => {
      const value = strategy[column];
      const opacity = 0.12 + 0.88 * value / maximum;
      addRect(315 + columnIndex * 150, y, 130, 30, '', {
        fill: column === 'failures' ? '#c62828' : '#1d70b8',
        opacity
      });
      addText(380 + columnIndex * 150, y + 21, value, 'chart-value', 'middle');
    });
  });
}

function renderHistogram() {
  const values = comparableHistory().map((run) => run.durationMs);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const binCount = Math.min(10, Math.max(5, Math.round(Math.sqrt(values.length))));
  const width = Math.max(1, max - min);
  const bins = Array.from({ length: binCount }, () => 0);
  values.forEach((value) => {
    const index = Math.min(binCount - 1, Math.floor((value - min) / width * binCount));
    bins[index] += 1;
  });
  const maximum = Math.max(...bins);
  bins.forEach((count, index) => {
    const x = 90 + index * 720 / binCount;
    const barWidth = 700 / binCount;
    const height = count * 250 / maximum;
    addRect(x, 350 - height, barWidth, height, 'series-primary', { opacity: 0.65 });
  });
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const deviation = Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length) || 1;
  const curve = [];
  for (let index = 0; index <= 100; index += 1) {
    const value = min + width * index / 100;
    const density = Math.exp(-0.5 * ((value - mean) / deviation) ** 2);
    curve.push([90 + 720 * index / 100, 350 - density * 230]);
  }
  elements.chart.appendChild(svgElement('path', {
    d: linePath(curve), fill: 'none', stroke: '#c62828', 'stroke-width': 4
  }));
  addLine(80, 350, 830, 350, 'axis');
}

function renderControlChart() {
  const runs = comparableHistory();
  const values = runs.map((run) => run.durationMs);
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const deviation = Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length);
  const upper = mean + 3 * deviation;
  const lower = Math.max(0, mean - 3 * deviation);
  const maximum = Math.max(1, upper, ...values);
  const x = (index) => 80 + index * 750 / Math.max(1, values.length - 1);
  const y = (value) => 365 - value * 300 / maximum;
  [['Mean', mean, '#1d70b8'], ['UCL', upper, '#c62828'], ['LCL', lower, '#c62828']]
    .forEach(([label, value, color]) => {
      addLine(70, y(value), 850, y(value), '', { stroke: color, 'stroke-dasharray': '8 6' });
      addText(845, y(value) - 6, label, 'chart-label', 'end');
    });
  const points = values.map((value, index) => [x(index), y(value)]);
  elements.chart.appendChild(svgElement('path', {
    d: linePath(points), fill: 'none', stroke: '#2e8540', 'stroke-width': 3
  }));
  points.forEach((point, index) => addCircle(point[0], point[1], 5, '', {
    fill: values[index] > upper || values[index] < lower ? '#c62828' : '#2e8540'
  }));
}

function renderScatter() {
  const runs = allVisibleRuns();
  const maxX = Math.max(1, ...runs.map((run) => run.metrics.discovered));
  const maxY = Math.max(1, ...runs.map((run) => run.durationMs));
  addLine(75, 360, 850, 360, 'axis');
  addLine(75, 50, 75, 360, 'axis');
  runs.forEach((run) => {
    const x = 75 + run.metrics.discovered * 750 / maxX;
    const y = 360 - run.durationMs * 290 / maxY;
    addCircle(x, y, 7, `series-${resultClass(run.result)}`, { opacity: 0.75 });
  });
  addText(460, 405, 'Discovered components', 'chart-label', 'middle');
  addText(18, 210, 'Duration', 'chart-label', 'middle');
}

function renderPercentiles() {
  const values = comparableHistory().map((run) => run.durationMs);
  const rows = [
    ['P10', percentile(values, 0.1)], ['P25', percentile(values, 0.25)],
    ['Median', percentile(values, 0.5)], ['P75', percentile(values, 0.75)],
    ['P90', percentile(values, 0.9)]
  ];
  const maximum = Math.max(1, ...rows.map((row) => row[1]));
  rows.forEach((row, index) => {
    const y = 70 + index * 68;
    addText(115, y + 20, row[0], 'chart-label', 'end');
    addRect(145, y, 620 * row[1] / maximum, 30, 'series-primary');
    addText(800, y + 21, formatDuration(row[1]), 'chart-value', 'end');
  });
}

function renderCandlesticks() {
  const buckets = new Map();
  comparableHistory().forEach((run) => {
    const date = String(run.endedAt).slice(0, 10);
    if (!buckets.has(date)) buckets.set(date, []);
    buckets.get(date).push(run);
  });
  const days = Array.from(buckets.entries()).sort((left, right) => left[0].localeCompare(right[0]));
  const maximum = Math.max(1, ...days.flatMap(([, runs]) => runs.map((run) => run.durationMs)));
  const y = (value) => 360 - value * 290 / maximum;
  days.forEach(([date, runs], index) => {
    runs.sort((left, right) => Date.parse(left.endedAt) - Date.parse(right.endedAt));
    const open = runs[0].durationMs;
    const close = runs.at(-1).durationMs;
    const high = Math.max(...runs.map((run) => run.durationMs));
    const low = Math.min(...runs.map((run) => run.durationMs));
    const x = 120 + index * 680 / Math.max(1, days.length - 1);
    const color = close <= open ? '#2e8540' : '#c62828';
    addLine(x, y(high), x, y(low), '', { stroke: color, 'stroke-width': 3 });
    addRect(x - 14, Math.min(y(open), y(close)), 28, Math.max(3, Math.abs(y(open) - y(close))), '', {
      fill: color
    });
    addText(x, 390, date.slice(5), 'chart-label', 'middle');
  });
}

function renderEventDensity() {
  const series = selectedRun.passSeries;
  const maxActions = Math.max(1, ...series.map((item) => item.actions));
  const maxTime = Math.max(1, ...series.map((item) => item.elapsedMs));
  addLine(75, 360, 850, 360, 'axis');
  series.forEach((item, index) => {
    const x = 90 + index * 730 / Math.max(1, series.length - 1);
    const height = item.actions * 230 / maxActions;
    addRect(x - 10, 360 - height, 20, height, 'series-primary', { opacity: 0.7 });
    const timeY = 360 - item.elapsedMs * 280 / maxTime;
    addCircle(x, timeY, 4, 'series-secondary');
  });
  addText(720, 45, 'Bars: actions', 'chart-label');
  addText(720, 65, 'Dots: elapsed time', 'chart-label');
}

function renderBuildDistribution() {
  const byBuild = new Map();
  allVisibleRuns().forEach((run) => {
    if (!byBuild.has(run.buildNumber)) byBuild.set(run.buildNumber, []);
    byBuild.get(run.buildNumber).push(run.durationMs);
  });
  const groups = Array.from(byBuild.entries());
  const maximum = Math.max(1, ...groups.flatMap(([, values]) => values));
  groups.forEach(([build, values], index) => {
    const x = 180 + index * 540 / Math.max(1, groups.length - 1);
    const y = (value) => 350 - value * 280 / maximum;
    const min = Math.min(...values);
    const max = Math.max(...values);
    const q1 = percentile(values, 0.25);
    const median = percentile(values, 0.5);
    const q3 = percentile(values, 0.75);
    addLine(x, y(max), x, y(min), 'axis');
    addRect(x - 35, y(q3), 70, y(q1) - y(q3), 'series-primary');
    addLine(x - 35, y(median), x + 35, y(median), '', { stroke: '#fff', 'stroke-width': 3 });
    addText(x, 385, build || 'unknown', 'chart-label', 'middle');
  });
}

function populateRunSelect() {
  elements.runSelect.textContent = '';
  const runs = dashboardState && dashboardState.runs || [];
  if (!runs.length) {
    const option = document.createElement('option');
    option.value = '';
    option.textContent = 'No completed run';
    elements.runSelect.appendChild(option);
    elements.runSelect.disabled = true;
    return;
  }
  elements.runSelect.disabled = false;
  runs.forEach((run, index) => {
    const option = document.createElement('option');
    option.value = run.runRef;
    const prefix = run.batch && run.batch.index ? `#${run.batch.index} · ` : '';
    option.textContent = `${prefix}${run.formRef} · ${run.result}`;
    elements.runSelect.appendChild(option);
    if (run.runRef === dashboardState.selectedRunRef || (!dashboardState.selectedRunRef && index === runs.length - 1)) {
      option.selected = true;
    }
  });
  selectedRun = runs.find((run) => run.runRef === elements.runSelect.value) || runs.at(-1);
}

function populateChartSelect() {
  const view = elements.viewSelect.value;
  const current = elements.chartSelect.value;
  elements.chartSelect.textContent = '';
  chartRegistry.filter((chart) => chart.group === view).forEach((chart) => {
    const availability = chart.availability();
    const option = document.createElement('option');
    option.value = chart.id;
    option.textContent = availability.ok ? chart.title : `${chart.title} — unavailable`;
    option.disabled = !availability.ok;
    option.title = availability.ok ? '' : availability.reason;
    elements.chartSelect.appendChild(option);
  });
  if (Array.from(elements.chartSelect.options).some((option) => option.value === current)) {
    elements.chartSelect.value = current;
  }
}

function renderSummary() {
  if (!selectedRun) {
    elements.contextText.textContent = 'No completed run is available yet.';
    return;
  }
  const metrics = selectedRun.metrics || {};
  const issues = issueTotal(selectedRun);
  const batchCount = dashboardState.mode === 'batch' ? dashboardState.runs.length : 1;
  elements.contextText.textContent = dashboardState.mode === 'batch'
    ? `Completed batch · ${batchCount} result${batchCount === 1 ? '' : 's'} · ${selectedRun.formRef} selected`
    : `Latest singleton · ${selectedRun.formRef}`;
  elements.resultValue.textContent = selectedRun.result.replaceAll('_', ' ');
  elements.resultDetail.textContent = selectedRun.failureCategory === 'none'
    ? 'Terminal result captured'
    : `Category: ${selectedRun.failureCategory.replaceAll('_', ' ')}`;
  elements.resultCard.className = `summary-card result-card ${resultClass(selectedRun.result)}`;
  elements.durationValue.textContent = formatDuration(selectedRun.durationMs);
  elements.fieldsValue.textContent = `${formatNumber(metrics.filled)} / ${formatNumber(metrics.discovered)}`;
  elements.issuesValue.textContent = formatNumber(issues);
  elements.issuesDetail.textContent = issues ? 'Review the findings and diagnostic ZIP' : 'No aggregate issues recorded';
  elements.interpretation.textContent = plainInterpretation(selectedRun);
  elements.formRefValue.textContent = selectedRun.formRef;
  elements.passesValue.textContent = formatNumber(metrics.passes);
  elements.attachmentsValue.textContent = formatNumber(metrics.attachmentsCompleted);
  elements.rowsValue.textContent = formatNumber(metrics.rowsAdded);
  elements.confirmationValue.textContent = selectedRun.confirmationCaptured ? 'Captured' : 'Not captured';
  elements.buildValue.textContent = selectedRun.buildNumber || '—';
}

function renderChart() {
  clearChart();
  elements.chartUnavailable.classList.add('hidden');
  const chart = chartRegistry.find((item) => item.id === elements.chartSelect.value) ||
    chartRegistry.find((item) => item.group === elements.viewSelect.value);
  if (!chart) return;
  elements.chartGroupLabel.textContent = chart.group;
  elements.chartTitle.textContent = chart.title;
  elements.chartDescription.textContent = chart.description;
  const availability = chart.availability();
  if (!availability.ok) {
    elements.chartUnavailable.textContent = availability.reason;
    elements.chartUnavailable.classList.remove('hidden');
    elements.interpretation.textContent = availability.reason;
    return;
  }
  chart.render();
  elements.interpretation.textContent = `${plainInterpretation(selectedRun)} ${chart.description}`;
}

function render() {
  populateRunSelect();
  elements.viewSelect.value = dashboardState && dashboardState.defaultView || 'simple';
  populateChartSelect();
  renderSummary();
  renderChart();
}

async function refresh() {
  const response = await chrome.runtime.sendMessage({ type: 'GET_DASHBOARD_STATE' });
  if (!response || !response.ok) {
    throw new Error(response && response.error ? response.error : 'Dashboard state is unavailable.');
  }
  dashboardState = response.state;
  render();
}

async function clearHistory() {
  if (!window.confirm('Clear all retained PID-free aggregate dashboard history?')) return;
  const response = await chrome.runtime.sendMessage({ type: 'CLEAR_DASHBOARD_HISTORY' });
  if (!response || !response.ok) {
    throw new Error(response && response.error ? response.error : 'Dashboard history could not be cleared.');
  }
  dashboardState = response.state;
  render();
}

elements.runSelect.addEventListener('change', () => {
  selectedRun = (dashboardState.runs || []).find((run) => run.runRef === elements.runSelect.value) || null;
  populateChartSelect();
  renderSummary();
  renderChart();
});
elements.viewSelect.addEventListener('change', () => {
  populateChartSelect();
  renderChart();
});
elements.chartSelect.addEventListener('change', renderChart);
elements.refreshButton.addEventListener('click', () => refresh().catch((error) => {
  elements.interpretation.textContent = error && error.message ? error.message : String(error);
}));
elements.clearHistoryButton.addEventListener('click', () => clearHistory().catch((error) => {
  elements.interpretation.textContent = error && error.message ? error.message : String(error);
}));
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') refresh().catch(() => undefined);
});

refresh().catch((error) => {
  elements.interpretation.textContent = error && error.message ? error.message : String(error);
});
