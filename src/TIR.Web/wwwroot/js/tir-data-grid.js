(function ($) {
    'use strict';

    // متن های پرکاربرد جدول به فارسی نمایش داده می شوند.
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

    // برای جدول های صفحه بندی شده، کنترل صفحه بعد و قبل همچنان سمت سرور است.
    function create(element, options) {
        var defaults = {
            rtlEnabled: true,
            showBorders: true,
            showColumnLines: true,
            rowAlternationEnabled: true,
            hoverStateEnabled: true,
            columnAutoWidth: true,
            columnMinWidth: 85,
            wordWrapEnabled: false,
            width: '100%',
            height: 480,
            allowColumnResizing: true,
            columnResizingMode: 'widget',
            allowColumnReordering: true,
            columnChooser: { enabled: true, mode: 'select' },
            searchPanel: { visible: true, width: 250, highlightSearchText: true,
                placeholder: 'جستجو در این صفحه...' },
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

    var columns = {
        employees: [
            { dataField: 'Id', caption: 'شناسه', width: 90 },
            { dataField: 'EmployeeCode', caption: 'کد کارمند', cssClass: 'mono' },
            { dataField: 'FirstName', caption: 'نام' },
            { dataField: 'LastName', caption: 'نام خانوادگی' },
            { dataField: 'IsDeleted', caption: 'وضعیت', dataType: 'boolean', cellTemplate: function (container, options) {
                badge(container, options.value ? 'حذف شده' : 'فعال', options.value ? 'text-bg-danger' : 'text-bg-success');
            } },
            { caption: 'عملیات', minWidth: 250, allowSorting: false, allowFiltering: false,
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
            { caption: 'عملیات', minWidth: 290, allowSorting: false, allowFiltering: false,
                allowGrouping: false, allowSearch: false, cellTemplate: function (container, options) {
                appendActions(container, 'attribute', options.data.Id);
            } }
        ],
        search: [
            { dataField: 'Id', caption: 'شناسه', width: 90 },
            { dataField: 'EmployeeCode', caption: 'کد', cssClass: 'mono' },
            { dataField: 'FullName', caption: 'نام و نام خانوادگی' },
            { dataField: 'HasStaleFormula', caption: 'وضعیت فرمول', dataType: 'boolean', cellTemplate: function (container, options) {
                badge(container, options.value ? 'نتیجه قدیمی دارد' : 'عادی', options.value ? 'text-bg-warning' : 'text-bg-success');
            } },
            { caption: 'عملیات', width: 130, allowSorting: false, allowFiltering: false,
                allowGrouping: false, allowSearch: false, cellTemplate: function (container, options) {
                appendActions(container, 'search', options.data.Id);
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
                // گزارش نهایی صفحه بندی مخصوص خود DataGrid دارد؛ بقیه صفحات از سرور صفحه می گیرند.
                paging: { enabled: kind === 'report', pageSize: 20 },
                pager: { visible: kind === 'report', showPageSizeSelector: true,
                    allowedPageSizes: [10, 20, 50, 100], showNavigationButtons: true, showInfo: true },
                noDataText: node.attr('data-empty-message') || 'رکوردی یافت نشد.'
            });
        });
    }

    window.TIRDataGrid = { create: create };
    $(mountStaticGrids);
})(jQuery);
