
function reloadDashboard() {
    const intakeIds = $('#dashboardIntakeId').val();
    const categories = $('#dashboardCategoryName').val();
    const statusCodes = $('#dashboardStatuses').val();
    const substatus = $('#dashboardSubStatus').val();
    const tags = $('#dashboardTags').val();
    const assignees = $('#dashboardAssignees').val();
    const dateFrom = $('#dateFrom').val();
    const dateTo = $('#dateTo').val();
    const params = {};
    if (intakeIds.length > 0) {
        params.intakeIds = intakeIds;
    }
    if (categories.length > 0) {
        params.categories = categories;
    }
    if (statusCodes.length > 0) {
        params.statusCodes = statusCodes;
    }
    if (substatus.length > 0) {
        params.substatus = substatus;
    }
    if (tags.length > 0) {
        params.tags = tags;
    }
    if (assignees.length > 0) {
        params.assignees = assignees;
    }
    if (dateFrom.length > 0) {
        params.dateFrom = dateFrom;
    }
    if (dateTo.length > 0) {
        params.dateTo = dateTo;
    }

    const chartConfigs = [
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationStatusCount,
            label: 'applicationStatus',
            readPolicy: 'GrantApplicationManagement.Dashboard.ApplicationStatusCount',
            count: 'count',
            title: 'Submissions by Status',
            chartId: 'applicationStatusChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getEconomicRegionCount,
            label: 'economicRegion',
            readPolicy: 'GrantApplicationManagement.Dashboard.EconomicRegionCount',
            count: 'count',
            title: 'Submissions by Economic Region',
            chartId: 'economicRegionChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationTagsCount,
            label: 'applicationTag',
            readPolicy: 'GrantApplicationManagement.Dashboard.ApplicationTagsCount',
            count: 'count',
            title: 'Application Tags Overview',
            chartId: 'applicationTagsChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationAssigneeCount,
            label: 'applicationAssignee',
            readPolicy: 'GrantApplicationManagement.Dashboard.ApplicationAssigneeCount',
            count: 'count',
            title: 'Application Assignee Overview',
            chartId: 'applicationAssigneeChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getRequestedAmountPerSubsector,
            label: 'subsector',
            readPolicy: 'GrantApplicationManagement.Dashboard.RequestedAmountPerSubsector',
            count: 'totalRequestedAmount',
            title: 'Total Funding Requested Per Sub-Sector',
            chartId: 'subsectorRequestedAmountChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getRequestApprovedCount,
            label: 'description',
            readPolicy: 'GrantApplicationManagement.Dashboard.RequestApprovedCount',
            count: 'amount',
            title: 'Requested Vs. Approved Funding',
            chartId: 'requestVsApprovedChart',
            chartOption: 'bar',
            width: 465,
            height: 300
        }
    ];

    const fetchPromises = chartConfigs
        .filter(config => abp.auth.isGranted(config.readPolicy))
        .map(config => config.fetchFunction(params)
            .then(data => ({
                config,
                labels: data.map(obj => obj[config.label]),
                counts: data.map(obj => obj[config.count])
            }))
        );

    Promise.all(fetchPromises).then(results => {
        results.forEach(({ config, labels, counts }) => {
            initializeChart(config, labels, counts);
        });
    });
}

let colorPalette;

fetch('./colorsPalette.json')
    .then(response => response.json())
    .then(data => {
        colorPalette = data.colors;
    });

reloadDashboard();

function generateCard(config) {
    if (document.getElementById(config.chartId)) {
        return; // Skip if the chart element already exists
    }

    const container = document.getElementById('dashboardContainer');

    const cardDiv = document.createElement('div');
    cardDiv.className = 'col-md-6 col-lg-4 col-sm-12 p-2 card-border-radius';

    const abpCard = document.createElement('div');
    const abpCardBody = document.createElement('div');
    const chartDiv = document.createElement('div');
    chartDiv.id = config.chartId;
    abpCardBody.className = 'card mb-3';
    chartDiv.className = 'card-body';

    abpCardBody.appendChild(chartDiv);
    abpCard.appendChild(abpCardBody);
    cardDiv.appendChild(abpCard);
    container.appendChild(cardDiv);
}

function initializeChart(config, labelsArray, dataArray) {
    generateCard(config);
    let myChart = echarts.init(document.getElementById(config.chartId), null, {
        width: config.width,
        height: config.height,
        renderer: 'svg',
        useDirtyRect: false,
    });

    let option;

    switch (config.chartOption) {
        case "bar":
            option = initializeBarChart(config, dataArray, labelsArray);
            break;
        case "pie":
            option = initializePieChart(config, dataArray, labelsArray);
            break;
    }

    if (option && typeof option === 'object') {
        myChart.setOption(option);
    }

    window.addEventListener('resize', myChart.resize);
}

function initializePieChart(config, dataArray, labelsArray) {
    let sum = dataArray?.reduce((partialSum, a) => partialSum + a, 0) ?? 0;
    if (config.chartId === 'subsectorRequestedAmountChart') {
        sum = formatCurrency(sum);
    }

    let data = [];
    dataArray.forEach((value, index) => data.push({
        'value': value, 'name': labelsArray[index]
    }));

    let formatter = '{a| {c}}\n {b| {b}}';
    if (config.chartId === 'subsectorRequestedAmountChart') {
        formatter = '{a| ${c} ({d}%)}\n {b| {b}}';
    }

    let rich = {
        a: {
            color: '#474543',
            fontWeight: 700,
            fontSize: 18,
            align: 'left',
            padding: 5,
        },
        b: {
            color: '#2D2D2D',
            fontWeight: 400,
            fontSize: 14,
            align: 'left',
        }
    };

    if (config.chartId === 'subsectorRequestedAmountChart') {
        rich = {
            a: {
                color: '#474543',
                fontWeight: 700,
                fontSize: 14,
                align: 'left',
            },
            b: {
                color: '#2D2D2D',
                fontWeight: 400,
                fontSize: 14,
                align: 'left',
            }
        };
    }

    let option = {
        textStyle: {
            fontFamily: 'BCSans'
        },
        responsive: true,
        title: {
            text: config.title,
            left: 'left',
            top: '0%',
        },
        graphic: [
            {
                type: 'text',
                left: 'center',
                bottom: '18%',
                cursor: "auto",
                style: {
                    text: sum,
                    color: '#474543',
                    fontWeight: 700,
                    fontSize: 32,
                    fontFamily: 'BCSans'
                }
            }
        ],
        series: [
            {
                type: 'pie',
                radius: ['65%', '71%'],
                center: ['50%', '90%'],
                padAngle: 3,
                itemStyle: {
                    borderRadius: 10
                },
                startAngle: 180,
                endAngle: 360,
                labelLine: {
                    length: 30,
                },
                label: {
                    formatter: formatter,
                    overflow: 'break',
                    rich: rich
                },
                data: data,
                colorBy: "data",
                color: colorPalette,
                silent: true,
                avoidLabelOverlap: true,
            }
        ],
    };
    return option;
}

function initializeBarChart(config, dataArray, labelsArray) {
    let x_axisLabel = {
        color: '#2D2D2D',
        fontWeight: 400,
        fontSize: 14
    }

    let y_axisLabel = {
        color: '#474543',
        fontWeight: 700,
        fontSize: 14,
        formatter: function (value) {
            return formatToCADCurrency(value);
        }
    }

    let option = {
        textStyle: {
            fontFamily: 'BCSans'
        },
        title: {
            text: config.title,
            left: 'left',
            top: '0%',
        },
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            },
            formatter: function (params) {
                let tooltipText = params[0].name + '<br/>';
                params.forEach(function (item) {
                    tooltipText += item.marker + item.seriesName + ': ' + formatToCADCurrency(item.value) + '<br/>';
                });
                return tooltipText;
            }
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
        },
        xAxis: [
            {
                type: 'category',
                data: labelsArray,
                axisTick: {
                    alignWithLabel: true
                },
                axisLabel: x_axisLabel
            }
        ],
        yAxis: [
            {
                type: 'value',
                axisLabel: y_axisLabel
            }
        ],
        series: [
            {
                name: 'Amount',
                type: 'bar',
                barWidth: '50%',
                data: dataArray,
                itemStyle: {
                    color: ({ name }) => {
                        const colors = {
                            'Requested Amount': '#F8BA47',
                            'Approved Amount': '#0288D1',
                        };
                        return colors[name] || '#0288D1';
                    }
                }
            }
        ]
    };

    return option;
}

function formatToCADCurrency(amount) {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 0
    }).format(amount);
}

function formatCurrency(num) {
    const units = [
        { value: 1e9, suffix: 'B' },
        { value: 1e6, suffix: 'M' }
    ];

    for (const { value, suffix } of units) {
        if (num >= value) {
            return `$${(num / value).toFixed(1).replace(/\.0$/, '')}${suffix}`;
        }
    }

    return `$${num.toFixed(2)}`;
}

$('#dashboardIntakeId').change(function () {
    const selectedValue = $(this).val();
    let intakeList = JSON.parse($('#dashboardIntakeList').text());
    let childDropdown = $('#dashboardCategoryName');
    childDropdown.empty();
    const filteredIntakes = intakeList.filter(intake => selectedValue.includes(intake.intakeId));
    const categories = Array.from(new Set(filteredIntakes.flatMap(intake => intake.categories)));
    $.each(categories, function (index, item) {
        childDropdown.append($('<option>', {
            value: item,
            text: item,
            selected: 'selected'
        }));
    });
    highlightSelected('dashboardCategoryName', 'CATEGORIES');
    reloadDashboard();
});

// These dropdowns always show a fixed label (e.g. "STATUS") rather than the
// current selection, so the filter bar keeps a constant width.
function setDropdownLabel(dropdownId, title) {
    $('#' + dropdownId)
        .next('.select2-container')
        .find('.select2-selection__rendered')
        .attr('title', title)
        .text(title);
}

function highlightSelected(dropdownId, title) {
    $('#' + dropdownId + ' option:selected').addClass('dt-button-active');
    $('#' + dropdownId + ' option:not(:selected)').removeClass('dt-button-active');
    setDropdownLabel(dropdownId, title);
}

// Select2 has no equivalent of bootstrap-select's actionsBox, so build one.
function addSelectAllControls($select) {
    const $dropdown = $('.select2-container--open .select2-dropdown');
    if ($dropdown.find('.dashboard-actionsbox').length) {
        return;
    }

    const $box = $('<div class="dashboard-actionsbox btn-group" role="group"></div>');
    const $selectAll = $('<button type="button" class="btn btn-sm btn-light">Select All</button>');
    const $deselectAll = $('<button type="button" class="btn btn-sm btn-light">Deselect All</button>');

    const setAll = (selected) => (event) => {
        event.preventDefault();
        event.stopPropagation();
        $select.find('option').prop('selected', selected);
        // Fires the inline onchange (reloadDashboard) plus our own change handlers.
        $select.trigger('change');
        syncResultRows($select);
    };

    $selectAll.on('click', setAll(true));
    $deselectAll.on('click', setAll(false));

    $box.append($selectAll, $deselectAll);
    $dropdown.prepend($box);

    // Select2 leaves --highlighted on the last option the mouse passed over,
    // because it doubles as the keyboard position. Left alone it looks like a
    // hover that never cleared, so drop it once the pointer leaves the list.
    // Select2 reapplies it on the next mousemove or arrow key.
    $dropdown.find('.select2-results')
        .off('mouseleave.dashboardFilters')
        .on('mouseleave.dashboardFilters', function () {
            $(this).find('.select2-results__option--highlighted')
                .removeClass('select2-results__option--highlighted');
        });
}

// Result rows are matched to their <option> by label. Select2 does not expose
// the row's data here (jQuery .data('data') is undefined), and its DOM id
// embeds the value in a way that is ambiguous once a value contains a dash.
function findOptionByLabel($select, label) {
    return $select.find('option').filter(function () {
        return $(this).text().trim() === label;
    }).first();
}

// Toggle one option through Select2's documented route: change the value on the
// underlying <select> and let it know. Deliberately not by faking a click on the
// result row, which would depend on Select2's internal event wiring.
function toggleOptionByLabel($select, label) {
    const $option = findOptionByLabel($select, label);
    if (!$option.length) {
        return;
    }

    const value = String($option.val());
    const selected = ($select.val() || []).map(String);
    const next = !selected.includes(value)
        ? selected.concat(value)
        : selected.filter(function (item) { return item !== value; });

    $select.val(next).trigger('change');
    syncResultRows($select);

    // Select2 sends the highlight back to the first result whenever the value
    // changes, which would make Enter unusable for stepping down the list.
    keepHighlightOn(label);
    focusSearch($select);
}

// Presentational only: Select2 exposes no API for the keyboard position, and it
// recalculates the highlight itself on the next arrow key.
function keepHighlightOn(label) {
    const $options = $('.select2-container--open .select2-results__option');

    $options.removeClass('select2-results__option--highlighted');
    $options.filter(function () {
        return $(this).text().trim() === label;
    }).first().addClass('select2-results__option--highlighted');
}

// Select2 re-renders the results a few tens of milliseconds after the value
// changes and highlights the first row again. The exact delay is not ours to
// rely on, so rather than guess at a timeout, watch the list and put the
// highlight back if it moves off the row the user is on. Re-applying only when
// it is actually wrong keeps this from looping on its own mutations.
function holdHighlight($select, label) {
    const results = document.querySelector('.select2-container--open .select2-results');
    if (!results || !window.MutationObserver) {
        return;
    }

    const observer = new MutationObserver(function () {
        const current = document.querySelector('.select2-container--open .select2-results__option--highlighted');
        if (!current || current.textContent.trim() !== label) {
            keepHighlightOn(label);
        }
    });

    observer.observe(results, { subtree: true, attributes: true, attributeFilter: ['class'] });

    // Long enough to outlast Select2's re-render, short enough that it is gone
    // well before the next keypress.
    setTimeout(function () {
        observer.disconnect();
    }, 300);
}

// Select2 renders the results list when the panel opens and does not redraw it
// on a programmatic change, so the ticks would otherwise show stale state.
// Repaint the rows in place rather than reopening the panel: closing and
// reopening rebuilds the list, which resets its scroll position and the
// keyboard highlight, and reads as the list jumping on every keypress.
function syncResultRows($select) {
    const selected = new Set(($select.val() || []).map(String));

    $('.select2-container--open .select2-results__option').each(function () {
        const $row = $(this);
        const $option = findOptionByLabel($select, $row.text().trim());
        if (!$option.length) {
            return;
        }

        const isSelected = selected.has(String($option.val()));
        $row.toggleClass('select2-results__option--selected', isSelected);
        $row.attr('aria-selected', isSelected ? 'true' : 'false');
    });
}

const SEARCH_PLACEHOLDER = 'Filter';

function searchFieldOf($select) {
    return $select.next('.select2-container').find('.select2-search__field')[0];
}

// Select2 drops focus out of the search field whenever an option is picked, so
// typing stops working until the field is clicked again. Clicking it toggles
// the panel shut, which makes filtering after a selection awkward. Put focus
// back instead, so the user can keep typing without touching the mouse.
function focusSearch($select) {
    const field = searchFieldOf($select);
    if (field) {
        field.focus();
    }
}

// The search field stands in for the label while the panel is open, so without
// a placeholder the control just looks empty. Select2 clears the attribute
// whenever the selection changes, by mouse or by keyboard, so put it back each
// time. A native placeholder already hides itself once the user types, which is
// exactly the behaviour wanted here.
function keepSearchPlaceholder(field) {
    if (!field) {
        return;
    }

    field.setAttribute('placeholder', SEARCH_PLACEHOLDER);

    if (!window.MutationObserver || field.dashboardPlaceholderWatched) {
        return;
    }
    field.dashboardPlaceholderWatched = true;

    new MutationObserver(function () {
        // Only writing when it differs, so this cannot react to its own change.
        if (field.getAttribute('placeholder') !== SEARCH_PLACEHOLDER) {
            field.setAttribute('placeholder', SEARCH_PLACEHOLDER);
        }
    }).observe(field, { attributes: true, attributeFilter: ['placeholder'] });
}

// Select2 closes the panel on Enter instead of ticking the highlighted option,
// even with closeOnSelect:false. WAI-ARIA's listbox pattern says Enter should
// toggle the focused option, so take Enter over.
//
// Registered on the capture phase deliberately. Select2 binds its own keydown
// through jQuery at init, so a normal handler runs *after* it: by then Select2
// has already reset the keyboard position to the first row and stopPropagation
// cannot undo that. Capture runs first, and stopImmediatePropagation keeps
// Select2 from seeing the key at all.
function bindEnterToToggle($select, searchField) {
    if (!searchField || searchField.dashboardEnterBound) {
        return;
    }
    searchField.dashboardEnterBound = true;

    searchField.addEventListener('keydown', function (event) {
        if (event.key !== 'Enter') {
            return;
        }

        const $highlighted = $('.select2-container--open .select2-results__option--highlighted');
        if (!$highlighted.length) {
            return;
        }

        const label = $highlighted.text().trim();
        event.preventDefault();
        event.stopImmediatePropagation();
        toggleOptionByLabel($select, label);
        holdHighlight($select, label);
    }, true);
}

function initDropdown(dropdownId, title) {
    const $select = $('#' + dropdownId);

    $select.select2({
        theme: 'bootstrap-5',
        width: '100%',
        closeOnSelect: false,
        allowClear: false,
        // Select2 appends dropdowns to <body>, so this class is what keeps the
        // dashboard styling off the other Select2 controls in the app.
        dropdownCssClass: 'dashboard-filter-dropdown'
    });

    // Fires for mouse selections; the keyboard path restores focus itself.
    // Deferred so it runs after Select2 has finished moving focus away.
    $select.on('select2:select select2:unselect', function () {
        setTimeout(function () {
            focusSearch($select);
        }, 0);
    });

    $select.on('select2:open', function () {
        addSelectAllControls($select);

        const $search = $select.next('.select2-container').find('.select2-search__field');
        keepSearchPlaceholder($search[0]);

        // Focus is deferred a tick on purpose: at select2:open the container
        // has not yet gained .select2-container--open, so the field is still
        // display:none per our CSS and silently refuses focus. Without focus,
        // arrow keys and Enter never reach Select2 and typing goes nowhere.
        setTimeout(function () {
            $search.trigger('focus');
        }, 0);

        bindEnterToToggle($select, $search[0]);
    });

    setDropdownLabel(dropdownId, title);
}

$(function () {

    initDropdown('dashboardIntakeId', 'INTAKES');
    initDropdown('dashboardCategoryName', 'CATEGORIES');
    initDropdown('dashboardStatuses', 'STATUS');
    initDropdown('dashboardSubStatus', 'SUB-STATUS');
    initDropdown('dashboardTags', 'TAGS');
    initDropdown('dashboardAssignees', 'ASSIGNEES');

    highlightSelected('dashboardIntakeId', 'INTAKES');
    highlightSelected('dashboardCategoryName', 'CATEGORIES');
    highlightSelected('dashboardStatuses', 'STATUS');
    highlightSelected('dashboardSubStatus', 'SUB-STATUS');
    highlightSelected('dashboardTags', 'TAG(S)');
    highlightSelected('dashboardAssignees', 'ASSIGNEE(S)');
    
    $('#dashboardIntakeId').change(function () {
        highlightSelected('dashboardIntakeId', 'INTAKES');
    });
    $('#dashboardCategoryName').change(function () {
        highlightSelected('dashboardCategoryName', 'CATEGORIES');
    });
    $('#dashboardStatuses').change(function () {
        highlightSelected('dashboardStatuses', 'STATUS');
    });
    $('#dashboardSubStatus').change(function () {
        highlightSelected('dashboardSubStatus', 'SUB-STATUS');
    });
    $('#dashboardTags').change(function () {
        highlightSelected('dashboardTags', 'TAGS');
    });
    $('#dashboardAssignees').change(function () {
        highlightSelected('dashboardAssignees', 'ASSIGNEES');
    });
});
