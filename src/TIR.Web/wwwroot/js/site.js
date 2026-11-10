(function ($) {
    'use strict';

    $(function () {
        var path = window.location.pathname.toLowerCase().replace(/\/+$/, '') || '/';

        var activeLink = null;
        var bestMatchLength = -1;

        $('.side-nav a').removeClass('active').each(function () {

            var href = ($(this).attr('href') || '')
                .toLowerCase()
                .replace(/\/+$/, '') || '/';

            if (href === path) {
                activeLink = this;
                return false;
            }

            if (href !== '/' &&
                href !== '/hangfire' &&
                path.startsWith(href + '/') &&
                href.length > bestMatchLength
            ) {
                activeLink = this;
                bestMatchLength = href.length;
            }
        });

        if (activeLink) {
            $(activeLink).addClass('active');
        }

        $('#menuToggle').on('click', function () {
            $('#appSidebar').toggleClass('open');
        });

        $(document).on('click', function (event) {
            if (!$(event.target).closest('#appSidebar,#menuToggle').length)
                $('#appSidebar').removeClass('open');
        });

        $(document).on('submit', 'form[data-confirm]', function (event) {
            if (!window.confirm($(this).attr('data-confirm')))
                event.preventDefault();
        });

        var pageHost = $('#tirPageHostModal');

        if (pageHost.length) {
            var pageFrame = $('#tirPageFrame');

            function synchronizeParentData() {
                // پس از ویرایش در پاپ آپ، مقادیر ستون های داینامیک جستجوی پیشرفته نیز تازه شوند.
                if ($('#employeeSearchGrid').length && $('#advancedSearchForm').length) {
                    $('#advancedSearchForm').trigger('submit');

                    return;
                }

                var gridNode = $('#employeesGrid,#attributesGrid').first();

                if (!gridNode.length && path !== '/') return;

                if (window.TIRLoading)
                    window.TIRLoading.show('در حال به روزرسانی فهرست...');

                $.get(window.location.href).done(function (markup) {
                    var snapshot = new DOMParser().parseFromString(markup, 'text/html');

                    if (!gridNode.length) {
                        var metricValues = snapshot.querySelectorAll('.metric-number');

                        $('.metric-number').each(function (index) {
                            if (metricValues[index])
                                $(this).text(metricValues[index].textContent);
                        });

                        return;
                    }

                    var dataId = gridNode.attr('data-data-id');
                    var nextData = snapshot.getElementById(dataId);

                    if (!nextData) return;

                    var actionPrefix = gridNode.is('#employeesGrid') ? 'employee' : 'attribute';
                    var newActions = snapshot.querySelectorAll('[id^="' + actionPrefix + '-actions-"]');
                    var actionHolder = gridNode.closest('.card').children('[hidden]').first();

                    actionHolder.empty();

                    Array.from(newActions).forEach(function (node) {
                        actionHolder.append(document.importNode(node, true));
                    });

                    var oldFooter = gridNode.closest('.card').children('.card-body').last();
                    var nextFooter = $(snapshot.getElementById(gridNode.attr('id')))
                        .closest('.card').children('.card-body').last();

                    if (oldFooter.length && nextFooter.length)
                        oldFooter.html(nextFooter.html());

                    var columnsId = gridNode.attr('data-columns-id');
                    var nextColumns = columnsId && snapshot.getElementById(columnsId);

                    if (nextColumns)
                        $('#' + columnsId).text(nextColumns.textContent);

                    gridNode.dxDataGrid('instance').option('dataSource', JSON.parse(nextData.textContent || '[]'));

                    if (window.TIRDataGrid && typeof window.TIRDataGrid.syncEmployeeColumns === 'function')
                        window.TIRDataGrid.syncEmployeeColumns(gridNode);

                    if (window.TIRDataGrid && typeof window.TIRDataGrid.refreshActionsWidth === 'function')
                        window.TIRDataGrid.refreshActionsWidth(gridNode);

                }).always(function () {
                    if (window.TIRLoading)
                        window.TIRLoading.hide();
                });
            }
            $(document).on('click', 'a[data-employee-details-modal],a[data-popup-page]', function (event) {
                if (event.which > 1 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;

                event.preventDefault();

                var target = new URL(this.href, window.location.href);
                target.searchParams.set('popup', '1');

                pageHost.data('changed', false);

                $('#tirPageTitle')
                    .text($(this).attr('title') ||
                        $(this).attr('aria-label') ||
                        $(this).text().trim() ||
                        'مدیریت اطلاعات');

                pageFrame.attr('src', target.toString());
                bootstrap.Modal.getOrCreateInstance(pageHost[0]).show();
            });

            $(window).on('message', function (event) {
                var original = event.originalEvent;

                if (original.origin !== window.location.origin ||
                    original.source !== pageFrame[0].contentWindow || !original.data) return;

                if (original.data.type === 'tir:popup-complete') {
                    pageHost.data('changed', true);
                    bootstrap.Modal.getOrCreateInstance(pageHost[0]).hide();

                    return;
                }

                if (original.data.type === 'tir:popup-changed') {
                    pageHost.data('changed', true);

                    return;
                }

                if (original.data.type !== 'tir:employee-details-saved') return;

                pageHost.data('changed', true);
                var gridElement = $('#employeeSearchGrid');

                if (gridElement.length && gridElement.data('dxDataGrid')) {
                    var instance = gridElement.dxDataGrid('instance');
                    var current = instance.option('dataSource');

                    if (Array.isArray(current)) {
                        current.forEach(function (row) {
                            if (row.Id !== original.data.id) return;

                            row.EmployeeCode = original.data.employeeCode;
                            row.FirstName = original.data.firstName;
                            row.LastName = original.data.lastName;
                        });

                        instance.refresh();
                    }
                }
            });

            pageHost.on('hidden.bs.modal', function () {
                pageFrame.attr('src', 'about:blank');

                if (pageHost.data('changed'))
                    synchronizeParentData();
            });
        }

        $('.js-filter-fields').on('input', function () {
            var query = $(this).val().toString().trim().toLowerCase();

            $(this).closest('.card').find('.field-grid > *').each(function () {
                $(this).toggle($(this).text().toLowerCase().includes(query));
            });
        });

        $(document).on('click', '.js-clear-field', function () {
            if (!window.confirm('این مقدار خالی شود؟')) return;

            var form = $(this).closest('form');
            form.find('.js-clear-value').val('true');
            form.trigger('submit');
        });

        $(document).on('submit', 'form[data-dynamic-save]', function (event) {
            event.preventDefault();

            var form = $(this), button = form.find('button[type=submit]');

            button.prop('disabled', true).text('در حال ذخیره...');

            $.ajax({
                url: form.attr('action'),
                type: 'POST',
                data: form.serialize(),
                success: function (answer) {
                    if (!answer.success) {
                        window.TIRMessages.error(answer.message);

                        return;
                    }

                    form.find('.js-rowversion').val(answer.rowVersion);

                    if (form.find('.js-clear-value').val() === 'true') {
                        form.find('[name="Field.RawValue"]').val('');
                        form.find('.js-bool-set,.js-bool-check').prop('checked', false);
                    }

                    form.trigger('tir:field-saved');
                    window.TIRMessages.success('مقدار ذخیره شد. وضعیت فرمول های وابسته را در صفحه جزئیات بررسی کنید.');
                },
                error: function (response) {
                    var info = response.responseJSON || {};
                    window.TIRMessages.error(info.message || 'خطای ارتباط با سرور رخ داد.');
                },
                complete: function () {
                    form.find('.js-clear-value').val('false');
                    button.prop('disabled', false).text('ذخیره این مقدار');
                }
            });

        });

        initializePersianPicker();
    });

    function initializePersianPicker() {
        if (!window.TirJalaliNavigation || !window.TirTimePicker) return;

        var popup = $('<div class="jalali-popup shadow"></div>').hide().appendTo('body');
        var timePicker = window.TirTimePicker.createPicker();
        var input = null;

        var calendar = window.TirJalaliNavigation.createCalendar(popup, {
            onSelectDay: function (year, month, day) {
                if (!input) return;

                var target = input;
                var selectedDate = [String(year).padStart(4, '0'),
                    String(month).padStart(2, '0'),
                    String(day).padStart(2, '0')].join('/');

                popup.hide();

                if (target.hasClass('js-jalali-datetime')) {
                    var parts = target.val().toString().trim().split(/\s+/);

                    timePicker.open(target, {
                        value: parts.length > 1 ? parts[1] : '',
                        onSelect: function (selectedTime) {
                            target.val(selectedDate + ' ' + selectedTime).trigger('change');
                        }
                    });
                }
                else {
                    target.val(selectedDate).trigger('change');
                }
            }
        });

        $(document).on('focus', '.js-jalali-date,.js-jalali-datetime', function () {
            input = $(this);
            timePicker.close();

            var raw = input.val().toString().replace(/[۰-۹٠-٩]/g, function (digit) {
                var code = digit.charCodeAt(0);

                return String.fromCharCode(48 + code - (code >= 0x06f0 ? 0x06f0 : 0x0660));
            });

            var date = raw.match(/(\d{4})\/(\d{1,2})\/(\d{1,2})/);

            if (date) {
                calendar.setDate(Number(date[1]), Number(date[2]), Number(date[3]));
            }
            else {
                calendar.resetToToday();
            }

            var position = input.offset();

            popup.css({
                position: 'absolute',
                top: position.top + input.outerHeight() + 5,
                left: position.left,
                zIndex: 1060
            }).show();

            var maxLeft = Math.max(10, $(window).width() - popup.outerWidth() - 10);
            popup.css('left', Math.min(Math.max(10, position.left), maxLeft));
        });

        $(document).on('focus click', '.js-tir-time', function () {
            var target = $(this);

            if (timePicker.isOpenFor(target)) return;

            popup.hide();
            timePicker.open(target, {
                value: target.val(),
                onSelect: function (selectedTime) {
                    target.val(selectedTime).trigger('change');
                }
            });
        });

        $(document).on('mousedown', function (event) {
            if (!$(event.target).closest('.jalali-popup,.js-jalali-date,.js-jalali-datetime').length)
                popup.hide();
        });

        $(document).on('keydown', function (event) {
            if (event.key === 'Escape' && popup.is(':visible'))
                popup.hide();
        });
    }
})(jQuery);
