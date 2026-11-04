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
            $(button).prop('disabled', busy).toggleClass('disabled', busy);
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
            var body = $('#bulkErrorTable tbody').empty();
            if (!items.length) {
                $('<tr>').append($('<td colspan="4" class="text-muted">').text('خطایی در این صفحه ثبت نشده است.')).appendTo(body);
                return;
            }
            items.forEach(function (entry) {
                var tr = $('<tr>');
                $('<td>').text(entry.excelRow ?? '—').appendTo(tr);
                $('<td>').text(entry.employeeId ?? '—').appendTo(tr);
                $('<td>').text(entry.inputOrdinal).appendTo(tr);
                $('<td>').text(entry.errorMessage).appendTo(tr);
                tr.appendTo(body);
            });
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
            var body = $('#bulkEmployeeSearchResults').empty();
            currentItems.forEach(function (item) {
                var tr = $('<tr>');
                var checkbox = $('<input type="checkbox" class="form-check-input bulk-employee-check">')
                    .attr('data-id', item.id).prop('checked', selectedIds.has(item.id) || selectionMode === 'filtered');
                $('<td>').append(checkbox).appendTo(tr);
                $('<td>').text(item.id).appendTo(tr);
                $('<td>').text(item.employeeCode).appendTo(tr);
                $('<td>').text((item.firstName || '') + ' ' + (item.lastName || '')).appendTo(tr);
                body.append(tr);
            });
            if (!currentItems.length)
                $('<tr>').append($('<td colspan="4" class="text-muted">').text('کارمندی یافت نشد.')).appendTo(body);
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
            $('#bulkEmployeeSearchResults').html('<tr><td colspan="4" class="text-muted">در حال دریافت نتایج...</td></tr>');
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
                .always(function () { $('#bulkSearchButton').prop('disabled', false); });
        }

        $('#bulkAdvancedSearchForm').on('submit', function (event) {
            event.preventDefault();
            searchEmployees();
        });
        $('#bulkOpenEmployeeModal').on('click', function () {
            modalMessage(null);
            if (!searchTotal) searchEmployees();
        });
        $('#bulkEmployeePrev').on('click', function () { if (searchPage > 1) getSearchPage(searchPage - 1); });
        $('#bulkEmployeeNext').on('click', function () {
            if (searchPage * 50 < searchTotal) getSearchPage(searchPage + 1);
        });
        $('#bulkEmployeeSearchResults').on('change', '.bulk-employee-check', function () {
            var id = Number($(this).data('id'));
            if (selectionMode === 'filtered') {
                // حالت انتخاب همه نتایج فقط با جستجوی تازه یا انتخاب صفحه تغییر می کند.
                $(this).prop('checked', true);
                return modalMessage('برای انتخاب دستی، دوباره جستجو کنید و کارمندان مورد نظر را انتخاب کنید.', 'info');
            }
            if ($(this).prop('checked')) selectedIds.add(id);
            else selectedIds.delete(id);
            renderSearch({ totalCount: searchTotal, items: currentItems });
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
