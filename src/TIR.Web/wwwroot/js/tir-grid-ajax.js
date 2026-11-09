(function ($) {
    'use strict';

    function notify(message, type) {
        window.TIRMessages.show(message, type || 'danger');
    }

    function messageFrom(xhr, fallback) {
        return xhr && xhr.responseJSON && xhr.responseJSON.message
            ? xhr.responseJSON.message : fallback;
    }

    function gridInstance(node) {
        if (!node.length) return null;
        try { return node.dxDataGrid('instance'); } catch (_) { return null; }
    }

    function applySnapshot(node, markup) {
        var doc = new DOMParser().parseFromString(markup, 'text/html');
        var updated = doc.getElementById(node.attr('id'));
        var json = doc.getElementById(node.attr('data-data-id'));
        var grid = gridInstance(node);
        if (!updated || !json || !grid) return false;

        var currentCard = node.closest('.card');
        var updatedCard = $(updated).closest('.card');
        var currentActions = currentCard.children('[hidden]').first();
        var updatedActions = updatedCard.children('[hidden]').first();
        if (currentActions.length && updatedActions.length)
            currentActions.html(updatedActions.html());
        var currentFooter = currentCard.children('.card-body').last();
        var updatedFooter = updatedCard.children('.card-body').last();
        if (currentFooter.length && updatedFooter.length)
            currentFooter.html(updatedFooter.html());
        var currentHeader = currentCard.children('.card-header').first();
        var updatedHeader = updatedCard.children('.card-header').first();
        if (currentHeader.length && updatedHeader.length)
            currentHeader.html(updatedHeader.html());

        node.attr('data-include-deleted', $(updated).attr('data-include-deleted') || 'false');
        grid.option('dataSource', JSON.parse(json.textContent || '[]'));
        return true;
    }

    function showLoading(node, text) {
        if (node.data('tirAjaxBusy')) return false;
        node.data('tirAjaxBusy', true);
        var grid = gridInstance(node);
        if (grid) grid.beginCustomLoading(text || 'در حال دریافت اطلاعات...');
        return true;
    }

    function hideLoading(node) {
        var grid = gridInstance(node);
        if (grid) grid.endCustomLoading();
        node.removeData('tirAjaxBusy');
    }

    function reload(node, request, targetUrl, checkBox) {
        if (!showLoading(node, 'در حال به روزرسانی گرید...')) return;
        var previous = node.attr('data-include-deleted') === 'true';
        if (checkBox) checkBox.option('disabled', true);
        $.ajax($.extend({ dataType: 'html' }, request))
            .done(function (markup) {
                if (!applySnapshot(node, markup)) {
                    if (checkBox) checkBox.option('value', previous);
                    if (node.is('#employeeSearchGrid'))
                        $('#IncludeDeleted').val(previous ? 'true' : 'false');
                    notify('پاسخ سرور حاوی داده های معتبر گرید نیست.');
                    return;
                }
                if (targetUrl)
                    window.history.replaceState(window.history.state, '',
                        targetUrl.pathname + targetUrl.search + targetUrl.hash);
            })
            .fail(function (xhr) {
                if (checkBox) checkBox.option('value', previous);
                if (node.is('#employeeSearchGrid'))
                    $('#IncludeDeleted').val(previous ? 'true' : 'false');
                notify(messageFrom(xhr, 'دریافت اطلاعات از سرور ناموفق بود.'));
            })
            .always(function () {
                hideLoading(node);
                if (checkBox) checkBox.option('disabled', false);
            });
    }

    function listUrl(node, includeDeleted) {
        var url = new URL(window.location.href);
        url.searchParams.delete('IncludeDeleted');
        url.searchParams.delete('PageNumber');
        if (node.is('#attributesGrid')) url.searchParams.delete('Search');
        if (includeDeleted) url.searchParams.set('IncludeDeleted', 'true');
        return url;
    }

    function advancedRequest() {
        var form = $('#advancedSearchForm');
        if (window.TIRAdvancedSearch && TIRAdvancedSearch.active)
            TIRAdvancedSearch.active.serialize();
        return { url: form.attr('action') || window.location.pathname,
            type: 'POST', data: form.serialize() };
    }

    function toggleDeleted(node, checkBox, includeDeleted) {
        var previous = node.attr('data-include-deleted') === 'true';
        if (includeDeleted === previous) return;
        if (node.is('#employeeSearchGrid')) {
            $('#IncludeDeleted').val(includeDeleted ? 'true' : 'false');
            $('#PageNumber').val('1');
            reload(node, advancedRequest(), null, checkBox);
        } else {
            var url = listUrl(node, includeDeleted);
            reload(node, { type: 'GET', url: url.toString() }, url, checkBox);
        }
    }

    function updateSearchResult(markup) {
        var doc = new DOMParser().parseFromString(markup, 'text/html');
        var fresh = doc.getElementById('employeeSearchGrid');
        var current = $('#employeeSearchGrid');
        var newSummary = $(doc).find('#advancedSearchForm .validation-summary-errors').first();
        var summary = $('#advancedSearchForm .validation-summary-errors, #advancedSearchForm .validation-summary-valid').first();
        if (summary.length) {
            if (newSummary.length) summary.replaceWith($(newSummary).clone());
            else summary.empty().removeClass('validation-summary-errors')
                .addClass('validation-summary-valid');
        }
        if (!fresh) {
            notify(newSummary.length ? newSummary.text().trim()
                : 'جستجو به نتیجه معتبر نرسید. شروط را بررسی کنید.');
            return false;
        }
        if (current.length) return applySnapshot(current, markup);

        $(fresh).closest('.card').insertBefore($('#attributeData'));
        if (window.TIRDataGrid) window.TIRDataGrid.mount();
        return true;
    }

    $(function () {
        $(document).on('submit', '.tir-data-grid .tir-grid-actions form', function (event) {
            if (event.isDefaultPrevented()) return;
            event.preventDefault();
            var form = $(this), node = form.closest('.tir-data-grid');
            var actionLabel = form.attr('data-grid-action') === 'restore' ? 'بازیابی' : 'حذف';
            function actionFailure(reason) {
                notify(actionLabel + ' با خطا مواجه شد' + (reason ? ': ' + reason : '.'));
            }
            if (!showLoading(node, 'در حال ثبت عملیات...')) return;
            var button = form.find('button[type="submit"]');
            button.prop('disabled', true);
            var finish = function () {
                hideLoading(node);
                button.prop('disabled', false);
            };
            $.ajax({
                url: form.attr('action'), type: 'POST', data: form.serialize(),
                dataType: 'json', headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).done(function (response) {
                if (!response.success) {
                    actionFailure(response.message);
                    finish();
                    return;
                }
                notify(response.message || 'عملیات انجام شد.', 'success');
                if (node.is('#employeeSearchGrid')) {
                    $.ajax($.extend({ dataType: 'html' }, advancedRequest()))
                        .done(function (markup) {
                            if (!updateSearchResult(markup)) notify('به روزرسانی جستجو ناموفق بود.');
                        }).fail(function (xhr) {
                            notify(messageFrom(xhr, 'به روزرسانی نتایج جستجو ممکن نشد.'));
                        }).always(finish);
                } else {
                    $.get(window.location.href).done(function (markup) {
                        if (!applySnapshot(node, markup)) notify('به روزرسانی گرید ناموفق بود.');
                    }).fail(function (xhr) {
                        notify(messageFrom(xhr, 'عملیات ثبت شد اما فهرست به روز نشد.'));
                    }).always(finish);
                }
            }).fail(function (xhr) {
                actionFailure(messageFrom(xhr, ''));
                finish();
            });
        });

        $(document).on('submit', '#employeeListFilters', function (event) {
            event.preventDefault();
            var url = new URL($(this).attr('action') || window.location.pathname, window.location.href);
            $(this).serializeArray().forEach(function (item) {
                if (item.value) url.searchParams.set(item.name, item.value);
            });
            if ($('#employeesGrid').attr('data-include-deleted') === 'true')
                url.searchParams.set('IncludeDeleted', 'true');
            reload($('#employeesGrid'), { type: 'GET', url: url.toString() }, url);
        });
        $(document).on('click', '.pagination a.page-link', function (event) {
            var node = $(this).closest('.card').find('.tir-data-grid').first();
            if (!node.length || node.is('#employeeSearchGrid')) return;
            event.preventDefault();
            var url = new URL(this.href, window.location.href);
            reload(node, { type: 'GET', url: url.toString() }, url);
        });

        $(document).on('submit', 'form[data-tir-ajax-report-delete]', function (event) {
            if (event.isDefaultPrevented()) return;
            event.preventDefault();
            var form = $(this);
            if (form.data('tirAjaxBusy')) return;
            form.data('tirAjaxBusy', true);
            var button = form.find('button[type="submit"],button:not([type])');
            button.prop('disabled', true);
            if (window.TIRLoading) TIRLoading.show('در حال پاکسازی درخواست...');
            $.ajax({
                url: form.attr('action'), type: 'POST', data: form.serialize(),
                dataType: 'json', headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).done(function (response) {
                if (!response.success) { notify(response.message); return; }
                $('#main-content > .toolbar, #main-content > .row, #main-content > .card').remove();
                var link = $('<a class="btn btn-primary mt-3">')
                    .attr('href', '/Bulk/Index').text('بازگشت به عملیات گروهی');
                $('#main-content').append(link);
                notify(response.message || 'اطلاعات گزارش حذف شدند.', 'success');
            }).fail(function (xhr) {
                notify(messageFrom(xhr, 'پاکسازی گزارش ممکن نشد.'));
            }).always(function () {
                if (window.TIRLoading) TIRLoading.hide();
                form.removeData('tirAjaxBusy');
                button.prop('disabled', false);
            });
        });

        $(document).on('submit', '#advancedSearchForm', function (event) {
            var submitter = event.originalEvent && event.originalEvent.submitter;
            if (submitter && /handler=Excel/i.test(submitter.getAttribute('formaction') || '')) return;
            event.preventDefault();
            var form = $(this);
            var old = $('#employeeSearchGrid');
            if (form.data('tirAjaxBusy')) return;
            form.data('tirAjaxBusy', true);
            var button = form.find('button[type="submit"]').first();
            button.prop('disabled', true);
            if (old.length) old.dxDataGrid('instance').beginCustomLoading('در حال جستجو...');
            $.ajax($.extend({ dataType: 'html' }, advancedRequest()))
                .done(updateSearchResult)
                .fail(function (xhr) { notify(messageFrom(xhr, 'اجرای جستجو ممکن نشد.')); })
                .always(function () {
                    if (old.length && gridInstance(old)) gridInstance(old).endCustomLoading();
                    form.removeData('tirAjaxBusy');
                    button.prop('disabled', false);
                });
        });
    });

    window.TIRGridAjax = { toggleDeleted: toggleDeleted, reload: reload };
})(jQuery);
