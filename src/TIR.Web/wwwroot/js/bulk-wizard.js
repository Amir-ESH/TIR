(function ($) {
    'use strict';

    $(function () {
        var urls = $('#bulkUrls').data();
        var token = $('input[name="__RequestVerificationToken"]').val();
        var selectionMode = 'selected';
        var selectedIds = new Set();
        var activeFilterJson = '[]';
        var activeEmployeeSearch = '';
        var activeSortBy = 'Id';
        var activeSortAttributeId = '';
        var activeSortDescending = false;
        var searchPage = 1;
        var searchTotal = 0;
        var currentItems = [];
        var employeeGrid = null;
        var errorGrid = null;
        var syncingGridSelection = false;
        var workId = null;
        var errorPage = 1;
        var timer = null;
        var isExecuting = false;
        var currentStep = 1;

        var searchBuilder = window.TIRAdvancedSearch.setup({
            form: '#bulkAdvancedSearchForm', attributeData: '#bulkAttributeData',
            container: '#bulkConditionsContainer', addCondition: '#bulkAddCondition',
            filtersJson: '#bulkFiltersJson', pagination: '#bulkUnusedPagination'
        });

        function number(value) { return Number(value || 0).toLocaleString('fa-IR'); }
        function operation() { return $('input[name="bulkOperation"]:checked').val(); }

        function message(value, type) {
            var box = $('#bulkWizardMessage');
            if (!value) { box.addClass('d-none').text(''); return; }
            box.removeClass('d-none alert-danger alert-success alert-info')
                .addClass('alert-' + (type || 'info')).text(value);
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        function modalMessage(value, type) {
            var box = $('#bulkModalMessage');
            if (!value) { box.addClass('d-none').text(''); return; }
            box.removeClass('d-none alert-danger alert-success alert-info')
                .addClass('alert-' + (type || 'danger')).text(value);
            var modalBody = $('#bulkEmployeeModal .modal-body');
            modalBody.scrollTop(0);
        }

        function errorText(xhr) {
            return xhr?.responseJSON?.error || xhr?.responseJSON?.message ||
                'ارتباط با سرور برقرار نشد. دوباره تلاش کنید.';
        }

        function switchStep(step) {
            currentStep = step;
            $('#bulkStep1,#bulkStep2,#bulkStep3').addClass('d-none');
            $('#bulkStep' + step).removeClass('d-none');
            $('#bulkWizardSteps .step').each(function () {
                $(this).toggleClass('is-current', Number($(this).data('step')) === step);
            });
            message(null);
            if (step === 3 && errorGrid) errorGrid.updateDimensions();
        }

        function applyOperation() {
            var mode = operation();
            $('#bulkExcelPanel').toggleClass('d-none', mode === 'formula');
            $('#bulkFormulaPanel').toggleClass('d-none', mode !== 'formula');
            $('#bulkOpenEmployeeList,#bulkOpenEmployeeSearch').toggleClass('d-none', mode !== 'update');
            $('#bulkUpdateExcelHint').toggleClass('d-none', mode !== 'update');
            $('#bulkDownloadTemplate').attr('href', urls.template + '&operation=' + encodeURIComponent(mode));
            $('#bulkChooseEmployeeBox').toggleClass('d-none', $('#bulkFormulaScope').val() !== 'selected');
        }

        function post(url, data) {
            return $.ajax({ url: url, method: 'POST', data: data,
                contentType: false, processData: false,
                headers: { RequestVerificationToken: token } });
        }

        function setBusy(button, busy) {
            $(button).prop('disabled', busy).toggleClass('disabled tir-loading-button', busy);
        }

        $('#bulkGoStep2').on('click', function () { applyOperation(); switchStep(2); });
        $('#bulkBackStep1').on('click', function () { switchStep(1); });
        $('#bulkBackStep2').on('click', function () {
            if (isExecuting) return;
            workId = null;
            switchStep(2);
        });
        $('#bulkFormulaScope').on('change', function () {
            $('#bulkChooseEmployeeBox').toggleClass('d-none', $(this).val() !== 'selected');
        });
        $('input[name="bulkOperation"]').on('change', applyOperation);

        $('#bulkPrepare').on('click', function () {
            var button = this;
            var data = new FormData();
            var mode = operation();
            var url = urls.prepare;
            if (mode === 'formula') {
                if (!$('#bulkFormulaId').val()) return message('ابتدا فرمول را انتخاب کنید.', 'danger');
                data.append('formulaId', $('#bulkFormulaId').val());
                if ($('#bulkFormulaScope').val() === 'all') data.append('selectionMode', 'all');
                else {
                    if (selectionMode !== 'filtered' && !selectedIds.size)
                        return message('حداقل یک کارمند را در پنجره انتخاب کنید.', 'danger');
                    data.append('selectionMode', selectionMode);
                    data.append('selectedIdsJson', JSON.stringify(Array.from(selectedIds)));
                    data.append('filtersJson', activeFilterJson);
                    data.append('employeeSearch', activeEmployeeSearch);
                }
                url = urls.prepareFormula;
            } else {
                var file = $('#bulkExcelFile')[0].files[0];
                if (!file) return message('ابتدا فایل اکسل را انتخاب کنید.', 'danger');
                if (!/\.xlsx$/i.test(file.name)) return message('فایل باید xlsx باشد.', 'danger');
                data.append('operationKind', mode === 'create' ? '1' : '2');
                data.append('excelFile', file);
            }
            setBusy(button, true);
            message(null);
            post(url, data).done(function (result) {
                workId = result.workId;
                errorPage = 1;
                $('#bulkPreparedSummary').text(result.message || 'درخواست آماده شده است.');
                $('#bulkTotal').text(number(result.total));
                $('#bulkProcessed,#bulkErrorCount').text(number(0));
                $('#bulkStatusLabel').text('آماده اجرا');
                $('#bulkExecute').prop('disabled', false).removeClass('d-none');
                $('#bulkBackStep2').prop('disabled', false);
                updateBar(0);
                switchStep(3);
            }).fail(function (xhr) { message(errorText(xhr), 'danger'); })
                .always(function () { setBusy(button, false); });
        });

        function updateBar(percent) {
            var value = Math.max(0, Math.min(100, Number(percent) || 0));
            $('#bulkProgressBar').css('width', value + '%')
                .toggleClass('progress-bar-animated', isExecuting);
            $('#bulkProgressPercent').text(number(value) + '%');
            $('.progress').attr('aria-valuenow', value);
        }

        function renderErrors(items) {
            if (!errorGrid) {
                // داده های هر صفحه از سرور می آیند و صفحه بندی سراسری حفظ می شود.
                errorGrid = window.TIRDataGrid.create('#bulkErrorGrid', {
                    dataSource: [],
                    columns: [
                        { dataField: 'excelRow', caption: 'ردیف اکسل', cellTemplate: function (node, info) { $(node).text(info.value ?? '—'); } },
                        { dataField: 'employeeId', caption: 'شناسه کارمند', cellTemplate: function (node, info) { $(node).text(info.value ?? '—'); } },
                        { dataField: 'inputOrdinal', caption: 'ردیف عملیات' },
                        { dataField: 'errorMessage', caption: 'شرح خطا', minWidth: 300 }
                    ],
                    noDataText: 'خطایی در این صفحه ثبت نشده است.'
                });
            }
            errorGrid.option('dataSource', items);
        }

        function refreshReport() {
            if (!workId) return;
            $.getJSON(urls.status, { workId: workId, errorPage: errorPage })
                .done(function (report) {
                    var h = report.header;
                    $('#bulkTotal').text(number(h.totalCount));
                    $('#bulkProcessed').text(number(h.processedCount));
                    $('#bulkErrorCount').text(number(h.failedCount));
                    // درصد فقط از شمارنده کل درخواست محاسبه می شود، نه از اندازه هر بخش 10 هزار تایی.
                    var percent = h.totalCount ? Math.floor(h.processedCount * 100 / h.totalCount) : 100;
                    updateBar(percent);
                    renderErrors(report.errors || []);
                    $('#bulkErrorPageLabel').text('صفحه ' + number(errorPage));
                    $('#bulkPrevError').prop('disabled', errorPage <= 1);
                    $('#bulkNextError').prop('disabled', (report.errors || []).length < 100);
                    var completed = h.status === 2 || h.status === 3 || h.status === 4;
                    $('#bulkStatusLabel').text(h.status === 2 ? 'تکمیل شد' :
                        h.status === 3 ? 'پایان با خطا' : h.status === 4 ? 'عملیات متوقف شد' :
                        h.status === 5 ? 'در صف پردازش' : 'در حال پردازش');
                    if (completed) {
                        isExecuting = false;
                        window.clearInterval(timer);
                        timer = null;
                        $('#bulkProgressBar').removeClass('progress-bar-animated');
                        $('#bulkExecute').removeClass('tir-loading-button').prop('disabled', true);
                        if (h.failureMessage) message(h.failureMessage, 'danger');
                        else if (h.status === 3) message('عملیات پایان یافت؛ خطاهای هر ردیف را در گزارش بررسی کنید.', 'info');
                    }
                }).fail(function () {
                    $('#bulkStatusLabel').text('خطا در دریافت گزارش؛ برای تلاش دوباره به روز رسانی را بزنید.');
                });
        }

        $('#bulkExecute').on('click', function () {
            if (!workId || isExecuting) return;
            var data = new FormData();
            data.append('workId', workId);
            setBusy(this, true);
            $('#bulkBackStep2').prop('disabled', true);
            var button = this;
            post(urls.execute, data).done(function () {
                isExecuting = true;
                $('#bulkStatusLabel').text('در صف پردازش');
                refreshReport();
                if (timer) window.clearInterval(timer);
                timer = window.setInterval(refreshReport, 1500);
            }).fail(function (xhr) {
                message(errorText(xhr), 'danger');
                $('#bulkBackStep2').prop('disabled', false);
                setBusy(button, false);
            });
        });

        $('#bulkRefresh').on('click', refreshReport);
        $('#bulkPrevError').on('click', function () { if (errorPage > 1) { errorPage--; refreshReport(); } });
        $('#bulkNextError').on('click', function () { errorPage++; refreshReport(); });

        function resetFilterSelection() {
            selectionMode = 'selected';
            selectedIds.clear();
            $('#bulkSelectPage').prop('checked', false);
        }

        function renderSearch(result) {
            searchTotal = result.totalCount;
            currentItems = result.items || [];
            if (!employeeGrid) {
                employeeGrid = window.TIRDataGrid.create('#bulkEmployeeSearchResults', {
                    dataSource: [], keyExpr: 'id',
                    // انتخاب ردیف ها از قابلیت اصلی DataGrid استفاده می کند.
                    selection: { mode: 'multiple', showCheckBoxesMode: 'always', allowSelectAll: false },
                    onSelectionChanged: function (event) {
                        if (syncingGridSelection) return;
                        if (selectionMode === 'filtered') {
                            modalMessage('برای انتخاب دستی، دوباره جستجو کنید و کارمندان مورد نظر را انتخاب کنید.', 'info');
                            renderSearch({ totalCount: searchTotal, items: currentItems });
                            return;
                        }
                        event.currentSelectedRowKeys.forEach(function (id) { selectedIds.add(id); });
                        event.currentDeselectedRowKeys.forEach(function (id) { selectedIds.delete(id); });
                        renderSearch({ totalCount: searchTotal, items: currentItems });
                    },
                    columns: [
                        { dataField: 'id', caption: 'شناسه', width: 100 },
                        { dataField: 'employeeCode', caption: 'کد کارمند' },
                        { caption: 'نام و نام خانوادگی', minWidth: 200,
                            calculateCellValue: function (row) { return (row.firstName || '') + ' ' + (row.lastName || ''); } }
                    ],
                    noDataText: 'کارمندی یافت نشد.'
                });
            }
            syncingGridSelection = true;
            employeeGrid.option('dataSource', currentItems);
            employeeGrid.option('selectedRowKeys', currentItems.filter(function (item) {
                return selectionMode === 'filtered' || selectedIds.has(item.id);
            }).map(function (item) { return item.id; }));
            syncingGridSelection = false;
            $('#bulkEmployeePagination').text('صفحه ' + number(searchPage) + ' از ' +
                number(Math.max(1, Math.ceil(searchTotal / 50))) + '، کل ' + number(searchTotal));
            $('#bulkEmployeePrev').prop('disabled', searchPage === 1);
            $('#bulkEmployeeNext').prop('disabled', searchPage * 50 >= searchTotal);
            $('#bulkSelectAllFiltered').prop('disabled', !searchTotal);
            $('#bulkSelectPage').prop('checked', Boolean(currentItems.length) &&
                currentItems.every(function (item) { return selectedIds.has(item.id) || selectionMode === 'filtered'; }));
            $('#bulkModalSelectionHint').text(selectionMode === 'filtered' ? 'همه نتایج جستجو انتخاب شده اند.' :
                number(selectedIds.size) + ' کارمند انتخاب شده است.');
        }

        function searchEmployees() {
            searchBuilder.serialize();
            activeFilterJson = $('#bulkFiltersJson').val() || '[]';
            activeEmployeeSearch = $('#bulkEmployeeSearch').val() || '';
            activeSortBy = $('#bulkSortBy').val() || 'Id';
            activeSortAttributeId = $('#bulkSortAttributeId').val() || '';
            activeSortDescending = $('#bulkSortDescending').val() === 'true';
            resetFilterSelection();
            searchTotal = 0;
            currentItems = [];
            renderSearch({ totalCount: 0, items: [] });
            employeeGrid.beginCustomLoading('در حال دریافت نتایج...');
            modalMessage(null);
            getSearchPage(1);
        }

        function getSearchPage(page) {
            searchPage = page;
            var data = new FormData();
            data.append('filtersJson', activeFilterJson);
            data.append('employeeSearch', activeEmployeeSearch);
            data.append('sortBy', activeSortBy);
            data.append('sortAttributeId', activeSortAttributeId);
            data.append('sortDescending', String(activeSortDescending));
            data.append('pageNumber', String(page));
            $('#bulkSearchButton').prop('disabled', true);
            modalMessage(null);
            post(urls.search, data).done(renderSearch)
                .fail(function (xhr) {
                    renderSearch({ totalCount: 0, items: [] });
                    modalMessage(errorText(xhr), 'danger');
                })
                .always(function () {
                    $('#bulkSearchButton').prop('disabled', false);
                    if (employeeGrid) employeeGrid.endCustomLoading();
                });
        }

        $('#bulkAdvancedSearchForm').on('submit', function (event) {
            event.preventDefault();
            searchEmployees();
        });
        $('#bulkOpenEmployeeModal').on('click', function () {
            modalMessage(null);
            if (!searchTotal) searchEmployees();
        });
        $('#bulkEmployeeModal').on('shown.bs.modal', function () {
            if (employeeGrid) employeeGrid.updateDimensions();
        });
        $('#bulkEmployeePrev').on('click', function () { if (searchPage > 1) getSearchPage(searchPage - 1); });
        $('#bulkEmployeeNext').on('click', function () {
            if (searchPage * 50 < searchTotal) getSearchPage(searchPage + 1);
        });
        $('#bulkSelectPage').on('change', function () {
            if (selectionMode === 'filtered') resetFilterSelection();
            var checked = $(this).prop('checked');
            currentItems.forEach(function (item) {
                if (checked) selectedIds.add(item.id);
                else selectedIds.delete(item.id);
            });
            renderSearch({ totalCount: searchTotal, items: currentItems });
        });
        $('#bulkSelectAllFiltered').on('click', function () {
            selectionMode = 'filtered';
            selectedIds.clear();
            renderSearch({ totalCount: searchTotal, items: currentItems });
        });
        $('#bulkConfirmEmployeeSelection').on('click', function () {
            if (selectionMode !== 'filtered' && !selectedIds.size)
                return modalMessage('حداقل یک کارمند را انتخاب کنید.', 'danger');
            $('#bulkSelectedEmployeeSummary').text(selectionMode === 'filtered' ?
                'تمام ' + number(searchTotal) + ' کارمند مطابق جستجو انتخاب شدند.' :
                number(selectedIds.size) + ' کارمند انتخاب شدند.');
            bootstrap.Modal.getOrCreateInstance(document.getElementById('bulkEmployeeModal')).hide();
        });

        applyOperation();
    });
})(jQuery);
