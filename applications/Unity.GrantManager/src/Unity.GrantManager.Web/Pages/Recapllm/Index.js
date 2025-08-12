
function getActiveExampleName() {
    const $activePanel = $('.left-card-body.active');
    if ($activePanel.length === 0) return null;          // no active panel
    return $.trim($activePanel.find('h1').first().text());
}

$(function () {
    // Show the first block on page-load
    $('.left-card-body').removeClass('active');     // make sure
    $('#grantCard').addClass('active');

    // Button click handler
    $('.card-switch').on('click', function () {
        const $btn = $(this);
        const target = $btn.data('target');         // "#grantCard" or "#licenceCard"

        // 1. swap button styles
        $('.card-switch').removeClass('active');
        $btn.addClass('active');

        // 2. show / hide panels
        $('.left-card-body').removeClass('active');
        $(target).addClass('active');

        document.getElementById("resultText").textContent = "";
        document.getElementById("finalResult").innerHTML = "";

        var active_example = getActiveExampleName();
        if (active_example == "Example Licence Application") {
            var result = `Food Safety and Compliance
- No detailed information on allergen controls or cross - contamination prevention.
- Lacks description of traceability or recall procedures beyond HACCP framework.
- No indication of frequency or protocol for staff retraining or compliance audits.

Facility Readiness and Technical Feasibility
- No detailed description of equipment, facility layout, or capacity limits.
- Absence of contingency plans for equipment failure or supply chain disruptions.
- No information on waste management systems beyond brine reuse and off - cuts diversion.
- No timeline or milestones for operational ramp - up or scaling production.

Environmental Sustainability
- Limited scope: no data on water or energy consumption metrics or reduction targets.
- No mention of lifecycle assessment or plans to improve packaging sustainability beyond recycled PET.
- Absence of greenhouse gas emissions management or carbon footprint reduction strategies.

Community and Economic Benefit
- Economic impact focused primarily on purchasing local produce; lacks detail on job creation or wage levels.
- No data on indirect benefits like training, local partnerships, or community engagement events.
- No mention of how food donations are tracked or their impact measured.

Workforce and Occupational Health & Safety
- No mention of workplace safety procedures or ergonomic considerations.
- No diversity, inclusion, or hiring equity policies shared.
- No training records or WHMIS / PPE protocols described.

Governance and Risk Management
- No outline of roles and responsibilities for food safety governance.
- No documentation of risk registers or business continuity plans.`;

            document.getElementById("resultText").textContent = result;
            document.getElementById("finalResult").innerHTML = result.replace(/\n/g, "<br>");
        } else if (active_example == "Example Expression of Interest") {
            var result = `Experience Depth:
- Limited detail on specific leadership roles or team sizes managed.
- No mention of challenges faced or lessons learned over 8 years.
- Lack of detail on the complexity of ML-powered features beyond ARR growth.

Skills Relevance:
- Missing direct references to specific AI or advanced data science methodologies beyond generative-AI prompt design.
- No mention of experience with certain advanced AI frameworks or platforms relevant to a Data & AI Platform.
- Absence of technical certifications or continuous learning related to emerging AI technologies.

Communication Clarity:
- Slightly fragmented presentation with bullet points lacking contextual flow.
- Limited examples illustrating storytelling or how insights impacted stakeholders.
- Some jargon and abbreviations (e.g., ARR, CSM) are not defined for clarity.

Cultural Alignment:
- Minimal insights on teamwork, collaboration style, or adaptability.
- General statement on excitement for ethical AI but no concrete examples of cultural fit or values alignment.
- No mention of diversity, equity, inclusion, or community involvement relevant to the organization's culture.

Strategic Thinking and Influence
- No mention of market sizing, competitive analysis, or pricing experimentation beyond one margin gain.
- Unclear if the patent submission led to adoption or remains conceptual.`;
            document.getElementById("resultText").textContent = result;
            document.getElementById("finalResult").innerHTML = result.replace(/\n/g, "<br>");
        }
    });
});

//Test function
function handleSubmit(event) {
    event.preventDefault(); 

    const resultText = document.getElementById("resultText").value;

    alert("Submitted text:\n" + resultText);


    // Just in case you also need to send it to a server endpoint using fetch:
    /*
    fetch('/api/submit', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ result: resultText })
    }).then(response => response.json())
      .then(data => console.log(data));
    */

    /* -----------------------------------------------------------
    CONFIG — replace with an environment-injected variable or
    proxy the call through your own server.
 ----------------------------------------------------------- */
    const OPENAI_API_KEY = 'API_KEY';

    /* -----------------------------------------------------------
       SAMPLE GRANT APPLICATION (same as Python example)
    ----------------------------------------------------------- */
    const example1 = `Legal Name of Organization
        GreenBridge Composites Incorporated
        Operating Name (if different)
        GreenBridge Composites
        Organization Type (e.g., for-profit corporation, not-for-profit, charity, public institution)
        Private, Canadian-controlled for-profit corporation (federally incorporated)
        Primary Contact – Name & Title
        Elena Ruiz – VP, Sustainability & Innovation
        Primary Contact – Email & Phone
        elena.ruiz@greenbridge.ca · +1 604 555 0134
        Project Title
        “Piloting Bio-Resin Bridge Deck Panels for Rural Infrastructure”
        Project Summary (100–150 words)
        GreenBridge Composites will design, prototype, and field-test modular bridge-deck panels made from plant-based bio-resins reinforced with recycled carbon fibre. The project aims to demonstrate a 40 % weight reduction and 25 % life-cycle emission cut versus conventional steel-reinforced concrete decks, while matching or exceeding CSA-S6-19 structural performance standards.
        Total Project Budget (CAD)
        $1,250,000
        Amount Requested from this Grant (CAD)
        $500,000
        Project Start & End Dates
        Start – 1 Oct 2025
        Project Location(s) (city, province/state, country)
        Design & fabrication: Richmond, BC • Field trial: Skeena-Queen Charlotte Regional District, BC
        Key Objectives (3–5 bullet points)
	        • Engineer bio-resin composite panels compatible with existing girder geometries
	        • Build a pilot production line capable of 50 m²/day output
	        • Install a 12-metre demonstration span on a rural logging road
	        • Collect structural-health-monitoring data for 12 months
        Expected Outcomes & Metrics (how success will be measured)
	        • Weight reduction ≥ 40 % (kg/m²) vs. concrete
	        • Greenhouse-gas cut ≥ 25 % (cradle-to-gate LCA)
	        • Load rating ≥ CL-625 per CSA-S6-19
	        • At least one provincial ministry adopts panels into approved products list by 2027
        Community / Economic Impact Statement (max 200 words)
        By enabling lighter, corrosion-proof bridge decks, this project reduces maintenance costs for remote communities, extends asset life, and lowers transport-related emissions (lighter decks mean smaller cranes and fewer site deliveries). The pilot span will serve as a proof-of-concept for over 600 aging single-lane timber bridges on BC’s secondary roads, many of which limit economic activity due to load restrictions. Successful commercialization could create 28 full-time manufacturing jobs in Richmond within three years and open a $75 million export market for sustainable infrastructure components.
        Previous Grant Funding Received in the Last 5 Years? (yes/no; if yes, list program name, year, $ amount)
        No – GreenBridge has not received government grant funding in the past five years.`;

    const example2 = `Legal Name of Business
        Pacific Harvest Ferments Ltd.
        Business Registration / Incorporation No.
        BC1234567
        Operating Name (if different)
        Pacific Harvest
        Licence Class Requested
        Class B — Secondary / Value-Add Processor
        Primary Contact — Name & Title
        Marcus Lee — Director of Operations
        Primary Contact — Email & Phone
        marcus.lee@pacificharvest.ca · +1 250 555 0172
        Facility Address
        2750 Quadra St, Victoria, BC  V8T 4G5
        Products to Be Manufactured (100–150 words)
        We produce small-batch fermented vegetable products — chiefly kimchi, sauerkraut,
        and seasonal pickles — sourced from Vancouver Island farms. All products are raw,
        probiotic, and free of artificial preservatives.
        Projected Annual Volume (kg / L)
        24 000 kg of finished product per year
        Food-Safety Plan Summary (max 200 words)
        Our HACCP-based food-safety plan identifies critical control points for brining,
        pH stabilization (≤ 4.1), and cool-chain logistics. We maintain ISO 22000-aligned
        SOPs, conduct quarterly third-party microbial testing, and train all staff in
        FoodSafe Level 2.
        Environmental Stewardship Measures (max 150 words)
        The facility runs on 100 % BC Hydro renewable electricity, reuses brine water for
        secondary cleaning, and diverts vegetable off-cuts to a local biodigester.
        Packaging is 80 % post-consumer recycled PET with a take-back scheme.
        Community & Economic Impact Statement (max 100 words)
        We purchase ≈ $180 k of produce annually from six Island growers and donate 2 %
        of output to local food banks, supporting food security and circular agriculture.
        Requested Licence Term
        3 years
        Previous Food-Safety Infractions in Past 5 Years? (yes/no — if yes, give details)
        No — zero infractions reported.
        Declaration & Signature of Authorized Officer
        Signed 14 July 2025 — Marcus Lee`;

    const example3 = `Legal Name of Applicant
    Arielle Marie Thompson
Preferred Name
    Ari Thompson
Position Applied For
    Senior Product Manager — Data & AI Platform
Primary Contact — Email & Phone
    ari.thompson@example.com · +1 604 555 0198
Professional Summary (120–150 words)
    Product leader with 8 years of experience shipping B2B SaaS and data‑platform products at scale.
    I blend a user‑centred mindset with strong analytical skills, having launched three ML‑powered
    features that grew ARR by 32 % collectively. My background in computer science plus an MBA
    enables me to translate technical innovation into commercial value while leading
    cross‑functional teams through discovery, delivery, and go‑to‑market.
Key Skills & Tools
        • Product discovery & road‑mapping (Jira, Productboard)
        • SQL / BigQuery, Tableau, Amplitude analytics
        • Generative‑AI prompt design & evaluation frameworks
        • Agile / Kanban facilitation (CSM certified)
        • Stakeholder storytelling & OKR alignment
Education
        • M.B.A., Sauder School of Business, UBC (2019)
        • B.Sc. Computer Science, University of Victoria (2014)
Selected Career Highlights
        • Scaled data‑pipeline product from 0 → 2.4 PB daily throughput in 18 months.
        • Led pricing pivot that boosted gross margin by 11 percentage points.
        • Filed provisional patent on model‑explainability UI workflow (2023).
Current Employer & Notice Period
    DataForge Inc. — 4‑week notice
Work Eligibility
    Canadian citizen; open to hybrid or remote within BC
Availability for Interviews
    Weekdays 9 AM – 2 PM PT · Evenings upon request
Salary Expectation (CAD)
    $130 – 145 k base + bonus
Additional Notes
    I’m especially excited about your organisation’s commitment to ethical AI and would love
    to discuss how my experience building transparent, user‑trust‑centric features can accelerate
    your roadmap.`;

    /* -----------------------------------------------------------
       Streaming helper
    ----------------------------------------------------------- */
    async function getStream(input, model, targetEl = null) {
        const res = await fetch("https://api.openai.com/v1/responses", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${OPENAI_API_KEY}`
            },
            body: JSON.stringify({ model, input, stream: true })
        });

        const reader = res.body.getReader();
        const decoder = new TextDecoder("utf-8");
        let output = "";

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            // OpenAI SSE: each line starts with "data: "
            const chunk = decoder.decode(value, { stream: true });
            chunk.trim().split("\n").forEach(line => {
                if (!line.startsWith("data:")) return;
                try {
                    const obj = JSON.parse(line.replace("data:", "").trim());
                    if (obj.delta) {
                        output += obj.delta;
                        if (targetEl) {
                            targetEl.textContent += obj.delta;
                            targetEl.scrollTop = targetEl.scrollHeight;
                        }
                    }
                } catch { /* ignore non-JSON keep-alive lines */ }
            });
        }
        return output;
    }

    /* -----------------------------------------------------------
       Main workflow — mirrors your Python main()
    ----------------------------------------------------------- */
    async function main(target) {
        target.textContent = "";           // clear previous run
        document.getElementById("finalResult").innerHTML = "";

        ///* 1) Generate 3 independent draft reviews */
        //const drafts = [];
        //for (var i = 0; i < 2; i++) {
        //    const result = await getStream(
        //        `You are a professional BC grant reviewer. Please review the following grant `
        //        + `application, and output the following metrics from 0 to 30 in JSON, along with `
        //        + `a rationale: [Financial Analysis, Economic Impact, Inclusive Growth, Clean Growth]. `
        //        + `This is the grant application: ${example1}`,
        //        "gpt-4.1-mini", target
        //    )
        //    drafts.push(result);
        //}

        ///* 2) Collate drafts into a single review (stream into target) */
        //const collated = await getStream(
        //    `You are a professional BC grant reviewer. Please take the following grant reviews `
        //    + `and combine them into a final answer, be concise and especially focus on summarizing missing elements of the proposal.`
        //    + `Do not output any preamble, only the review itself for each metric. `
        //    + `For each review please give bullet points for each positive or negative aspect of the application, and label them accordingly.`
        //    + `These are the metrics being graded out of 30: `
        //    + `[Financial Analysis, Economic Impact, Inclusive Growth, Clean Growth]. `
        //    + `And this is the grant application: ${example1}. `
        //    + `These are the preliminary reviews: ${JSON.stringify(drafts)}`,
        //    "gpt-4.1-mini",
        //    target            // stream live into div
        //);

        const active_example = getActiveExampleName();
        var example_text = "";
        var example_metrics = "";
        if (active_example == "Example Licence Application") {
            example_text = example2;
            example_metrics = "[Food Safety and Compliance, Facility Readiness and Technical Feasibility, Environmental Sustainability, Community and Economic Benefit]"
            example_prompt = "You are a professional licensing agent. Please review the following food manufacturing licence."
        } else if (active_example == "Example Grant Application") {
            example_text = example1;
            example_metrics = "[Eligibility, Project Need, Project Benefits, Project Timeline, Project Budget, Project Risk & Feasibility, Community Support, Diversity, Inclusion and Reconciliation, Risk Ranking, Forest Impacts Assessment, Adjudicator Summary & Comments]";
            example_prompt = "You are a professional BC grant reviewer. Please review the following grant application."
        } else if (active_example == "Example Expression of Interest") {
            example_text = example3;
            example_metrics = "[Experience Depth, Skills Relevance, Communication Clarity, Cultural Alignment]";
            example_prompt = "You are a professional hiring manager. Please review the following expression of interest."
        }

        /* 2) Collate drafts into a single review (stream into target) */
        const collated = await getStream(
            `${example_prompt} Be concise and especially focus on summarizing missing elements of the proposal.`
            + `Do not output any preamble, only the review itself for each metric. `
            + `For each metric please give a list of negative or missing aspects of the application.`
            + `These are the metrics being graded out of 30: `
            + `${example_metrics}. `
            + `And this is the grant application: ${example_text}. `,
            "gpt-4.1-mini",
            target            // stream live into div
        );

        /* 3) Improve the collated review (stream into same target) */
        //const final = await getStream(
        //    `You are a professional BC grant reviewer. These are the metrics being graded `
        //    + `out of 30: [Financial Analysis, Economic Impact, Inclusive Growth, Clean Growth]. `
        //    + `Please review and improve upon the following grant review: ${collated}, `
        //    + `outputting only the new review, with applicable BC policy frameworks. Be concise and especially focus on summarizing missing elements of the proposal.`
        //    + `Do not output any preamble, only the review itself for each metric`,
        //    "o3-mini-2025-01-31",
        //    target            // continues streaming
        //);
        document.getElementById("finalResult").innerHTML = collated.replace(/\n/g, "<br>");
    }
    main(document.getElementById("resultText"));

}
