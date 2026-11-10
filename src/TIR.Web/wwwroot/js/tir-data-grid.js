(function ($) {
    'use strict';

    if (window.DevExpress && DevExpress.localization) {
        DevExpress.localization.loadMessages({ fa: {
            'dxDataGrid-noDataText': 'داده ای برای نمایش وجود ندارد.',
            'dxDataGrid-searchPanelPlaceholder': 'جستجو در این صفحه...',
            'dxDataGrid-groupPanelEmptyText': 'برای گروه بندی، عنوان ستون را اینجا بکشید.',
            'dxDataGrid-columnChooserTitle': 'انتخاب ستون ها',
            'dxDataGrid-columnChooserEmptyText': 'ستونی برای انتخاب وجود ندارد.',
            'dxDataGrid-pagerInfoText': 'صفحه {0} از {1} ({2} رکورد)',
            'dxDataGrid-pagerPageSize': 'تعداد در صفحه',
            'dxDataGrid-filterRowShowAllText': '(همه)',
            'dxDataGrid-filterRowResetOperationText': 'پاک کردن فیلتر',
            'dxDataGrid-filterRowOperationEquals': 'برابر',
            'dxDataGrid-filterRowOperationNotEquals': 'نابرابر',
            'dxDataGrid-filterRowOperationContains': 'شامل',
            'dxDataGrid-filterRowOperationNotContains': 'شامل نباشد',
            'dxDataGrid-filterRowOperationStartsWith': 'شروع با',
            'dxDataGrid-filterRowOperationEndsWith': 'پایان با'
        } });
        DevExpress.localization.locale('fa');
    }

    // سازنده جدول
    function create(element, options) {
        var defaults = {
            rtlEnabled: true, // فعال سازی راست چین
            showBorders: true, // نمایش بردر گرید
            showColumnLines: true, // نمایش خط بین ستون ها
            rowAlternationEnabled: true, // متفاوت کردن ظاهر ردیف ها یکی در میان
            hoverStateEnabled: true, // تغییر ظاهر ردیف زیر نشانگر ماوس
            columnAutoWidth: true, // تنظیم خودکار عرض ستون ها
            columnMinWidth: 85, // حداقل عرض پیشفرض ستون
            wordWrapEnabled: false, // جایگیری کل متن در ستون
            width: '100%', // استفاده از عرض فضای والد
            height: 500, // ارتفاع اولیه جدول
            allowColumnResizing: true, // امکان تغییر عرض ستون
            columnResizingMode: 'widget', // مدل تغییر سایز ستون
            allowColumnReordering: true, // امکان جابه‌جایی ستون‌ها

            columnChooser: { // تنظیمات انتخاب گر نمایش ستون در گرید
                enabled: true, // وضعیت نمایش انتخابگر ستون
                mode: 'select' // نوع انتخابگر
            },

            searchPanel: { // تنظیمات کادر جستجوی بالای پنل
                visible: true, // نمایش کادر جستجوی بالای گرید
                width: 250, // عرض کادر جستجو
                highlightSearchText: true, // مارک کردن متن جستجو شده در داخل گرید
                placeholder: 'جستجو در جدول زیر...' // متن نمایشی داخل کادر جستجو
            },

            filterRow: { // تنظیمات فیلتر زیر هر ستون
                visible: true, // فعال یا غیر فعال بودن فیلتر
                applyFilter: 'auto', // مدل تایید فیلتر
                showOperationChooser: true // نمایش انتخابگر عملگر ها
            }, 

            headerFilter: { visible: true }, // فعال بودن فیلتر عنوان ستون

            groupPanel: { // بخش گروه بندی گرید
                visible: true, // فعال بودن گروه بندی گرید
                emptyPanelText: 'برای گروه بندی ستون را اینجا کشیده و رها کنید.' // متن مکان گروه بندی
            },

            grouping: { // تنظیمات گروه بندی داخل گرید
                autoExpandAll: false, // بصورت خودکار باز شده یا جمع شده
                contextMenuEnabled: true // 
            },

            scrolling: { // تنظیمات اسکرول داخل گرید
                mode: 'standard', // مدل اسکرول
                showScrollbar: 'always', // نمایش اسکرول بار بصورت پیشفرض همیشه نمایش دهد
                useNative: true // استفاده از اسکرول بار طبیعی مرورگر
            },

            paging: { enabled: false }, // صفحه بندی داخلی بصورت پیشفرض خاموش
            pager: { visible: false }, // 
            sorting: { mode: 'multiple' }, // امکان مرتب سازی با چند ستون
            loadPanel: { enabled: true }, // نمایش وضعیت بارگذاری
            noDataText: 'داده ای برای نمایش وجود ندارد.' // متن نمایشی برای زمانی که داده ای برای نمایش نیست
        };

        return $(element)
            // ایجاد دیتا گرید بر روی المنت مورد نظر
            // دیفالت تنظیمات پیشفرض ما در کل پروژه است
            // آپشن تنظیمات مخصوص همان گرید هست
            .dxDataGrid(
                $.extend(true, {}, defaults, options) // ترکیب تنظیمات پیشفرض و مخصوص گرید
            )
            .dxDataGrid('instance'); // برگرداندن آبجیکت گرید ساخته شده
    }

    function appendActions(container, kind, id) {
        var source = $('#' + kind + '-actions-' + id);
        var actions = $('<div class="tir-grid-actions">').appendTo(container);

        if (source.length)
            source.children().clone().appendTo(actions);
    }

    function badge(container, label, className) {
        $('<span>').addClass('badge ' + className).text(label).appendTo(container);
    }

    function actionsWidth(node) {
        var kind = node.attr('data-grid-kind');
        var prefix = kind === 'employees' ? 'employee' : 'attribute';
        var maxCount = 0;
        node.closest('.card').children('[hidden]').first()
            .find('[id^="' + prefix + '-actions-"]').each(function () {
            maxCount = Math.max(maxCount, $(this).children('a,form,button').length);
        });

        return Math.max(70, Math.ceil(28 + maxCount * 34 + Math.max(0, maxCount - 1) * 6.4));
    }

    function refreshActionsWidth(node) {
        node = $(node);

        if (node.attr('data-grid-kind') !== 'employees' &&
            node.attr('data-grid-kind') !== 'attributes') return;

        var grid = node.data('dxDataGrid') && node.dxDataGrid('instance');

        if (!grid) return;

        var width = actionsWidth(node);

        if (grid.columnOption('tirActions', 'width') === width &&
            grid.columnOption('tirActions', 'minWidth') === width) return;

        grid.beginUpdate();

        try {
            grid.columnOption('tirActions', 'minWidth', width);
            grid.columnOption('tirActions', 'width', width);
        }
        finally {
            grid.endUpdate();
        }
    }

    function createEmployeeFieldGrid(element, rows, renderValue) {
        return create(element, {
            keyExpr: 'AttributeId', dataSource: rows,
            height: Math.min(650, Math.max(440, window.innerHeight - 205)),
            columnAutoWidth: false, wordWrapEnabled: true,
            columns: [
                {
                    dataField: 'Code',
                    caption: 'کد مشخصه', width: 175
                },
                {
                    dataField: 'Name',
                    caption: 'عنوان فارسی', minWidth: 200
                },
                {
                    dataField: 'TypeTitle',
                    caption: 'نوع فیلد', width: 145
                },
                {
                    caption: 'مقدار',
                    minWidth: 300,
                    allowSorting: false,
                    allowFiltering: false,
                    allowGrouping: false,
                    allowSearch: false,

                    cellTemplate: function (container, options) {
                        renderValue(container, options.data);
                    }
                }
            ],
            paging: { enabled: false },
            pager: { visible: false },
            sorting: { mode: 'multiple' },
            filterRow: {
                visible: true,
                applyFilter: 'auto'
            },
            headerFilter: { visible: true },
            searchPanel: {
                visible: true,
                width: 250,
                highlightSearchText: true
            },
            groupPanel: { visible: true },
            columnChooser: {
                enabled: true,
                mode: 'select'
            },
            allowColumnReordering: true,
            scrolling: {
                mode: 'standard',
                showScrollbar: 'always',
                useNative: true
            },
            noDataText: 'مشخصه ای برای نمایش وجود ندارد.'
        });
    }

    var columns = {
        employees: [
            {
                dataField: 'Id',
                caption: 'شناسه',
                width: 90,
                visible: false,
                showInColumnChooser: true
            },
            {
                dataField: 'EmployeeCode',
                caption: 'کد پرسنلی',
                cssClass: 'mono'
            },
            {
                dataField: 'FirstName',
                caption: 'نام'
            },
            {
                dataField: 'LastName',
                caption: 'نام خانوادگی'
            },
            {
                dataField: 'IsDeleted',
                caption: 'وضعیت',
                dataType: 'boolean',
                cellTemplate: function (container, options) {
                    badge(container,
                        options.value ? 'حذف شده' : 'فعال',
                        options.value ? 'text-bg-danger' : 'text-bg-success');
                }
            },
            {
                name: 'tirActions',
                caption: 'عملیات',
                width: 135,
                minWidth: 70,
                allowResizing: false,
                cssClass: 'tir-operations-column',
                allowSorting: false,
                allowFiltering: false,
                allowGrouping: false,
                allowSearch: false,
                showInColumnChooser: false,
                cellTemplate: function (container, options) {
                    appendActions(container, 'employee', options.data.Id);
                }
            }
        ],
        attributes: [
            {
                dataField: 'AttributeCode',
                caption: 'کد',
                cssClass: 'mono'
            },
            {
                dataField: 'Title',
                caption: 'عنوان'
            },
            {
                dataField: 'DataKindTitle',
                caption: 'نوع'
            },
            {
                dataField: 'SourceKindTitle',
                caption: 'منبع'
            },
            {
                dataField: 'IsRequired',
                caption: 'الزامی',
                dataType: 'boolean',
                cellTemplate: function (container, options) {
                    $(container).text(options.value ? 'بله' : 'خیر');
                }
            },
            {
                dataField: 'DisplayOrder',
                caption: 'ترتیب',
                width: 90
            },
            {
                dataField: 'IsDeleted',
                caption: 'وضعیت',
                dataType: 'boolean',
                cellTemplate: function (container, options) {
                    badge(container,
                        options.value ? 'حذف شده' : 'فعال',
                        options.value ? 'text-bg-danger' : 'text-bg-success');
                }
            },
            {
                name: 'tirActions',
                caption: 'عملیات',
                width: 135,
                minWidth: 70,
                allowResizing: false,
                cssClass: 'tir-operations-column',
                allowSorting: false,
                allowFiltering: false,
                allowGrouping: false,
                allowSearch: false,
                showInColumnChooser: false,
                cellTemplate: function (container, options) {
                    appendActions(container, 'attribute', options.data.Id);
                }
            }
        ],
        report: [
            {
                dataField: 'InputOrdinal',
                caption: 'ردیف کارمند'
            },
            {
                dataField: 'EmployeeId',
                caption: 'شناسه'
            },
            {
                dataField: 'AttributeId',
                caption: 'مشخصه'
            },
            {
                dataField: 'ErrorCode',
                caption: 'کد خطا',
                cssClass: 'mono'
            },
            {
                dataField: 'ErrorMessage',
                caption: 'دلیل',
                minWidth: 280
            }
        ]
    };

    // مشخصه های داینامیک در هر دو گرید کارمندان از یک تعریف مشترک ساخته می شوند.
    // پیش فرض همه ستون های داینامیک مخفی است و کاربر از انتخاب ستون آنها را فعال می کند.
    function employeeColumns(node, visibleColumns) {
        var metadata = $('#' + node.attr('data-columns-id')).text();
        var attributes = metadata ? JSON.parse(metadata) : [];
        var dynamicColumns = attributes.map(function (attribute) {
            var field = 'Attribute_' + attribute.id;
            return {
                name: field,
                dataField: field,
                caption: attribute.title || attribute.code,
                visible: !!(visibleColumns && visibleColumns[field]),
                showInColumnChooser: true,
                minWidth: 150,
                allowGrouping: true,
                cssClass: 'tir-dynamic-attribute-column'
            };
        });

        return columns.employees.slice(0, -1).concat(dynamicColumns, columns.employees.slice(-1));
    }

    function syncEmployeeColumns(node) {
        node = $(node);

        if (node.attr('data-grid-kind') !== 'employees') return;

        var grid = node.data('dxDataGrid') && node.dxDataGrid('instance');

        if (!grid) return;

        var metadata = $('#' + node.attr('data-columns-id')).text() || '[]';

        if (node.data('tirEmployeeColumnsMetadata') === metadata) return;

        var visibleColumns = {};

        (grid.option('columns') || []).forEach(function (column) {
            if (column.dataField && /^Attribute_\d+$/.test(column.dataField) && column.visible)
                visibleColumns[column.dataField] = true;
        });

        node.data('tirEmployeeColumnsMetadata', metadata);
        grid.option('columns', employeeColumns(node, visibleColumns));
        refreshActionsWidth(node);
    }

    // مسئول پیدا کردن داده ها از HTML صفحه
    function mountStaticGrids() {
        $('.tir-data-grid[data-grid-kind]').each(function () {
            var node = $(this);

            if (node.data('dxDataGrid')) return;

            var kind = node.attr('data-grid-kind');
            var dataId = node.attr('data-data-id');

            if (!columns[kind] || !dataId) return;

            var rows = JSON.parse($('#' + dataId).text() || '[]');

            // ساخت گرید
            create(node, {
                keyExpr: kind === 'report' ? undefined : 'Id',
                dataSource: rows,
                columns: kind === 'employees' ? employeeColumns(node) : columns[kind],
                paging: {
                    enabled: kind === 'report',
                    pageSize: 20
                },
                pager: {
                    visible: kind === 'report',
                    showPageSizeSelector: true,
                    allowedPageSizes: [10, 20, 50, 100],
                    showNavigationButtons: true,
                    showInfo: true
                },
                onContentReady: function () { refreshActionsWidth(node); },
                onToolbarPreparing: function (event) {
                    if (kind !== 'attributes' && kind !== 'employees') return;

                    var items = event.toolbarOptions.items || [];

                    var index = items.findIndex(function (item) {
                        return item.name === 'columnChooserButton';
                    });

                    if (index < 0) {
                        items.push({ name: 'columnChooserButton', location: 'after' });
                        index = items.length - 1;
                    }

                    if (items.some(function (item) {
                        return item.cssClass === 'tir-show-deleted-toggle';
                    })) return;

                    items.splice(index, 0, {
                        location: 'after',
                        widget: 'dxCheckBox',
                        cssClass: 'tir-show-deleted-toggle',
                        options: {
                            rtlEnabled: true,
                            text: 'نمایش حذف شده ها',
                            value: node.attr('data-include-deleted') === 'true',
                            onValueChanged: function (changed) {
                                if (changed.value === (node.attr('data-include-deleted') === 'true')) return;

                                if (window.TIRGridAjax && typeof window.TIRGridAjax.toggleDeleted === 'function') {
                                    window.TIRGridAjax.toggleDeleted(node, changed.component, changed.value);
                                }
                                else {
                                    changed.component.option('value', node.attr('data-include-deleted') === 'true');
                                }
                            }
                        }
                    });

                    event.toolbarOptions.items = items;
                },
                noDataText: node.attr('data-empty-message') || 'رکوردی یافت نشد.'
            });

            if (kind === 'employees')
                node.data('tirEmployeeColumnsMetadata', $('#' + node.attr('data-columns-id')).text() || '[]');
        });
    }

    window.TIRDataGrid = {
        create: create,
        createEmployeeFieldGrid: createEmployeeFieldGrid,
        mount: mountStaticGrids,
        refreshActionsWidth: refreshActionsWidth,
        syncEmployeeColumns: syncEmployeeColumns
    };

    $(mountStaticGrids);
})(jQuery);
