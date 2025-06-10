$(function () {

    const l = abp.localization.getResource('GrantManager');
    let dt = $('#ReconciliationTable');
    let submissions = [];
    unity.grantManager.intakes.submission.getSubmissionsList(true).then(function (pagedResultDto) { 
        submissions = pagedResultDto.items.filter(x => x.formSubmissionStatusCode === "SUBMITTED");

        // Build unique category options after submissions are loaded
        const categories = [...new Set(submissions.map(s => s.category))].filter(Boolean);
        const options = categories.map(c => ({ value: c, text: c }));

        // Add options to the select element
        const categoriesSelect = document.getElementById("ReconciliationCategoryFilter");
        if (categoriesSelect) {
            options.forEach(function(opt) {
                const option = document.createElement('option');
                option.value = opt.value;
                option.text = opt.text;
                categoriesSelect.appendChild(option);
            });
        }
    });

    let inputAction = function (requestData, dataTableSettings) {
        return false;
    };


    $('#search').on('input', function () {
        let table = $('#ReconciliationTable').DataTable();
        table.search($(this).val()).draw();
    });

    function onSubmissionSummaryFilterChanged() {
        let dateTo = new Date($('#dateTo').val());
        let dateFrom = new Date($('#dateFrom').val());

        let filtered_submissions = submissions.filter(x =>
            x.tenant.toLowerCase().includes($('#ReconciliationTenantFilter').val().toLowerCase()) &&
            (isNaN(dateTo.getTime()) || new Date(x.createdAt) <= dateTo) &&
            (isNaN(dateFrom.getTime()) || new Date(x.createdAt) >= dateFrom) &&
            (x.category == $("#ReconciliationCategoryFilter").val() || $("#ReconciliationCategoryFilter").val() == null)
        );

        totalSubmissions = filtered_submissions.length
        chefOnlySubmissions = filtered_submissions.filter(x => x.inUnity === false).length

        $('#ChefsSubmissionCount').html(totalSubmissions);
        $('#UnitySubmissionCount').html(totalSubmissions - chefOnlySubmissions);
        $('#MissingCount').html(chefOnlySubmissions);
    }
    window.onSubmissionSummaryFilterChanged = onSubmissionSummaryFilterChanged;


    let filterData = {"Status": "Missing"};

    let iDt = $('#ReconciliationTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, "asc"]],
            searching: true,
            externalSearchInputId: `#search`,
            scrollX: true,
            ajax:
                abp.libs.datatables.createAjax(
                    unity.grantManager.intakes.submission.getSubmissionsList, inputAction),
            columnDefs: [
                {
                    title: l('Chefs ID'), 
                    data: "confirmationId",
                    render: function (data) {
                        return data;
                    }
                },
                {
                    title: l('Applicant Name'),
                    data: "name",
                    render: function (data) {
                        return data;
                    }
                },
                {
                    title: l('Chefs Form Name'),
                    data: "form",
                    render: function (data) {
                        return data;
                    }
                },
                {
                    title: l('GrantApplicationStatus'),
                    data: "status",
                    render: function (data, type, row) {
                        if (row.formSubmissionStatusCode === 'SUBMITTED') {
                            return '<span class="badge bg-danger">Missing</span>';
                        } else {
                            return '<span class="badge bg-secondary">Draft</span>';
                        }
                    }
                },
                {
                    title: l('Creation Date'),
                    data: "createdAt",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString();
                    }
                },
                {
                    title: l('Tenant'),
                    data: "tenant",
                    render: function (data) {
                        return data;
                    }
                }
            ],
            processing: true,
            stateSaveParams: function (settings, data) {
                let searchValue = $(settings.oInit.externalSearchInputId).val();
                data.search.search = searchValue;

                let hasFilter = data.columns.some(value => value.search.search !== '') || searchValue !== '';
                $('#btn-toggle-filter').text(hasFilter ? FilterDesc.With_Filter : FilterDesc.Default);
            },
            stateLoadParams: function (settings, data) {
                $(settings.oInit.externalSearchInputId).val(data.search.search);

                data.columns.forEach((column, index) => {
                    if (settings.aoColumns[index] + "" != "undefined") {
                        const title = settings.aoColumns[index].sTitle;
                        const value = column.search.search;
                        filterData[title] = value;
                    }
                });
            }
        })
    );

    updateFilter(iDt, dt[0].id, filterData);

    iDt.on('column-reorder.dt', function (e, settings) {
        updateFilter(iDt, dt[0].id, filterData);
    });
    iDt.on('column-visibility.dt', function (e, settings, deselectedcolumn, state) {
        updateFilter(iDt, dt[0].id, filterData);
    });

    initializeFilterButtonPopover(iDt);

    searchFilter(iDt);

    setExternalSearchFilter(iDt);

    // Prevent row selection when clicking on a link inside a cell
    iDt.on('user-select', function (e, dt, type, cell, originalEvent) {
        if (originalEvent.target.nodeName.toLowerCase() === 'a') {
            e.preventDefault();
        }
    });


    function setExternalSearchFilter(dataTableInstance) {
        let searchId = dataTableInstance.init().externalSearchInputId;

        // Exclude default search inputs that have custom logic
        if (searchId !== false && searchId !== '#search') {
            $('.dataTables_filter input').attr("placeholder", "Search");
            $('.dataTables_filter label')[0].childNodes[0].remove();

            $(searchId).on('input', function () {
                let filter = dataTableInstance.search($(this).val()).draw();
                console.info(`Filter on #${searchId}: ${filter}`);
            });
        }
    }

    function updateFilter(dt, dtName, filterData) {
        let optionsOpen = false;
        $("#tr-filter").each(function () {
            if ($(this).is(":visible"))
                optionsOpen = true;
        })
        $('.tr-toggle-filter').remove();
        let newRow = $("<tr class='tr-toggle-filter' id='tr-filter'>");

        dt.columns().every(function () {
            let column = this;
            if (column.visible()) {
                let title = column.header().textContent;
                if (title && title !== 'Actions') {

                    let filterValue = filterData[title] ? filterData[title] : '';

                    let input = $("<input>", {
                        type: 'text',
                        class: 'form-control input-sm custom-filter-input',
                        placeholder: title,
                        value: filterValue
                    });

                    let newCell = $("<td>").append(input);

                    if (column.search() !== filterValue) {
                        column.search(filterValue).draw();
                    }

                    newCell.find("input").on("keyup", function () {
                        if (column.search() !== this.value) {
                            column.search(this.value).draw();
                            updateFilterButton(dt);
                        }
                    });

                    newRow.append(newCell);
                }
                else {
                    let newCell = $("<td>");
                    newRow.append(newCell);
                }
            }
        });

        updateFilterButton(dt);

        $(`#${dtName} thead`).after(newRow);

        if (optionsOpen) {
            $(".tr-toggle-filter").show();
        }
    }

    function searchFilter(iDt) {
        let searchValue = $(iDt.init().externalSearchInputId).val();
        if (searchValue) {
            iDt.search(searchValue).draw();
        }

        if ($('#btn-toggle-filter').text() === FilterDesc.With_Filter) {
            $(".tr-toggle-filter").show();
        }
    }

    function updateFilterButton(dt) {
        let searchValue = $(dt.init().externalSearchInputId).val();
        let columnFiltersApplied = false;
        dt.columns().every(function () {
            let search = this.search();
            if (search) {
                columnFiltersApplied = true;
            }
        });

        let hasFilter = columnFiltersApplied || searchValue !== '';
        $('#btn-toggle-filter').text(hasFilter ? FilterDesc.With_Filter : FilterDesc.Default);
    }

    $('.data-table-select-all').click(function () {

        if ($('.data-table-select-all').is(":checked")) {
            PubSub.publish('datatable_select_all', true);
        } else {
            PubSub.publish('datatable_select_all', false);
        }

    });

    // Toggle hidden export buttons for Ctrl+Alt+Shift+E globally
    $(document).keydown(function (e) {
        if (e.ctrlKey && e.altKey &&
            e.shiftKey && e.key === 'E') {
            // Toggle d-none class on elements with hidden-export class
            $('.hidden-export-btn').toggleClass('d-none');

            // Prevent default behavior
            e.preventDefault();
            return false;
        }
    });
});
