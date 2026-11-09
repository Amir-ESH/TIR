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

    function create(element, options) {
        var defaults = {
            rtlEnabled: true, // فعال سازی راست چین
            showBorders: true, // نمایش بردر گرید
            showColumnLines: true, // نمایش خط ستون ها
            rowAlternationEnabled: true,
            hoverStateEnabled: true,
            columnAutoWidth: true,
            columnMinWidth: 85,
            wordWrapEnabled: false,
            width: '100%',
            height: 500,
            allowColumnResizing: true,
            columnResizingMode: 'widget',
            allowColumnReordering: true,
            columnChooser: { enabled: true, mode: 'select' },
            searchPanel: { visible: true, width: 250, highlightSearchText: true,
                placeholder: 'جستجو در جدول زیر...' },
            filterRow: { visible: true, applyFilter: 'auto', showOperationChooser: true },
            headerFilter: { visible: true },
            groupPanel: { visible: true, emptyPanelText: 'برای گروه بندی ستون را اینجا بکشید.' },
            grouping: { autoExpandAll: false, contextMenuEnabled: true },
            scrolling: { mode: 'standard', showScrollbar: 'always', useNative: true },
            paging: { enabled: false },
            pager: { visible: false },
            sorting: { mode: 'multiple' },
            loadPanel: { enabled: true },
            noDataText: 'داده ای برای نمایش وجود ندارد.'
        };
        return $(element).dxDataGrid($.extend(true, {}, defaults, options)).dxDataGrid('instance');
    }

    function appendActions(container, kind, id) {
        var source = $('#' + kind + '-actions-' + id);
        var actions = $('<div class="tir-grid-actions">').appendTo(container);
        if (source.length) source.children().clone().appendTo(actions);
    }

    function badge(container, label, className) {
        $('<span>').addClass('badge ' + className).text(label).appendTo(container);
    }

    // عرض عملیات دقیقاً بر اساس بیشترین تعداد دکمه های همان گرید محاسبه می شود.
    function actionsWidth(kind) {
        var prefix = kind === 'employees' ? 'employee' : 'attribute';
        var maxCount = 0;
        $('[id^="' + prefix + '-actions-"]').each(function () {
            maxCount = Math.max(maxCount, $(this).children('a,form,button').length);
        });
        return Math.max(70, Math.ceil(20 + maxCount * 34 + Math.max(0, maxCount - 1) * 6.4));
    }

    function createEmployeeFieldGrid(element, rows, renderValue) {
        return create(element, {
            keyExpr: 'AttributeId', dataSource: rows,
            height: Math.min(650, Math.max(440, window.innerHeight - 205)),
            columnAutoWidth: false, wordWrapEnabled: true,
            columns: [
                { dataField: 'Code', caption: 'کد مشخصه', width: 175 },
                { dataField: 'Name', caption: 'عنوان فارسی', minWidth: 200 },
                { dataField: 'TypeTitle', caption: 'نوع فیلد', width: 145 },
                { caption: 'مقدار', minWidth: 300, allowSorting: false,
                    allowFiltering: false, allowGrouping: false, allowSearch: false,
                    cellTemplate: function (container, options) { renderValue(container, options.data); } }
            ],
            paging: { enabled: false }, pager: { visible: false },
            sorting: { mode: 'multiple' },
            filterRow: { visible: true, applyFilter: 'auto' },
            headerFilter: { visible: true },
            searchPanel: { visible: true, width: 250, highlightSearchText: true },
            groupPanel: { visible: true },
            columnChooser: { enabled: true, mode: 'select' },
            allowColumnReordering: true,
            scrolling: { mode: 'standard', showScrollbar: 'always', useNative: true },
            noDataText: 'مشخصه ای برای نمایش وجود ندارد.'
        });
    }

    var columns = {
        employees: [
            { dataField: 'Id', caption: 'شناسه', width: 90, visible: false, showInColumnChooser: true },
            { dataField: 'EmployeeCode', caption: 'کد پرسنلی', cssClass: 'mono' },
            { dataField: 'FirstName', caption: 'نام' },
            { dataField: 'LastName', caption: 'نام خانوادگی' },
            { dataField: 'IsDeleted', caption: 'وضعیت', dataType: 'boolean', cellTemplate: function (container, options) {
                badge(container, options.value ? 'حذف شده' : 'فعال', options.value ? 'text-bg-danger' : 'text-bg-success');
            } },
            { name: 'tirActions', caption: 'عملیات', width: 135, minWidth: 70,
                allowResizing: false, cssClass: 'tir-operations-column',
                allowSorting: false, allowFiltering: false,
                allowGrouping: false, allowSearch: false, cellTemplate: function (container, options) {
                appendActions(container, 'employee', options.data.Id);
            } }
        ],
        attributes: [
            { dataField: 'AttributeCode', caption: 'کد', cssClass: 'mono' },
            { dataField: 'Title', caption: 'عنوان' },
            { dataField: 'DataKindTitle', caption: 'نوع' },
            { dataField: 'SourceKindTitle', caption: 'منبع' },
            { dataField: 'IsRequired', caption: 'الزامی', dataType: 'boolean', cellTemplate: function (container, options) {
                $(container).text(options.value ? 'بله' : 'خیر');
            } },
            { dataField: 'DisplayOrder', caption: 'ترتیب', width: 90 },
            { dataField: 'IsDeleted', caption: 'وضعیت', dataType: 'boolean', cellTemplate: function (container, options) {
                badge(container, options.value ? 'حذف شده' : 'فعال', options.value ? 'text-bg-danger' : 'text-bg-success');
            } },
            { name: 'tirActions', caption: 'عملیات', width: 135, minWidth: 70,
                allowResizing: false, cssClass: 'tir-operations-column',
                allowSorting: false, allowFiltering: false,
                allowGrouping: false, allowSearch: false, cellTemplate: function (container, options) {
                appendActions(container, 'attribute', options.data.Id);
            } }
        ],
        report: [
            { dataField: 'InputOrdinal', caption: 'ردیف کارمند' },
            { dataField: 'EmployeeId', caption: 'شناسه' },
            { dataField: 'AttributeId', caption: 'مشخصه' },
            { dataField: 'ErrorCode', caption: 'کد خطا', cssClass: 'mono' },
            { dataField: 'ErrorMessage', caption: 'دلیل', minWidth: 280 }
        ]
    };

    function mountStaticGrids() {
        $('.tir-data-grid[data-grid-kind]').each(function () {
            var node = $(this);
            var kind = node.attr('data-grid-kind');
            var dataId = node.attr('data-data-id');
            if (!columns[kind] || !dataId) return;
            var rows = JSON.parse($('#' + dataId).text() || '[]');
            create(node, {
                keyExpr: kind === 'report' ? undefined : 'Id',
                dataSource: rows,
                columns: columns[kind],
                paging: { enabled: kind === 'report', pageSize: 20 },
                pager: { visible: kind === 'report', showPageSizeSelector: true,
                    allowedPageSizes: [10, 20, 50, 100], showNavigationButtons: true, showInfo: true },
                onContentReady: function (event) {
                    if (kind !== 'employees' && kind !== 'attributes') return;
                    var width = actionsWidth(kind);
                    if (event.component.columnOption('tirActions', 'width') !== width)
                        event.component.columnOption('tirActions', 'width', width);
                },
                onToolbarPreparing: function (event) {
                    if (kind !== 'attributes') return;
                    var items = event.toolbarOptions.items || [];
                    var index = items.findIndex(function (item) { return item.name === 'columnChooserButton'; });
                    if (index < 0) index = items.length;
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
                                reloadAttributes(node, event.component, changed.component, changed.value);
                            }
                        }
                    });
                    event.toolbarOptions.items = items;
                },
                noDataText: node.attr('data-empty-message') || 'رکوردی یافت نشد.'
            });
        });
    }

    window.TIRDataGrid = { create: create, createEmployeeFieldGrid: createEmployeeFieldGrid };
    $(mountStaticGrids);
})(jQuery);
