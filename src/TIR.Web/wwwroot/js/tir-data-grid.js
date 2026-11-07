(function ($) {
    'use strict';

    // تمام جدول های برنامه با یک تنظیم مشترک DevExtreme راه اندازی می شوند.
    function create(element, options) {
        var defaults = {
            rtlEnabled: true,
            showBorders: true,
            showColumnLines: false,
            rowAlternationEnabled: true,
            hoverStateEnabled: true,
            columnAutoWidth: true,
            columnMinWidth: 85,
            wordWrapEnabled: true,
            width: '100%',
            scrolling: { mode: 'standard', showScrollbar: 'onHover' },
            paging: { enabled: false },
            pager: { visible: false },
            sorting: { mode: 'none' },
            loadPanel: { enabled: false },
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
            { dataField: 'IsDeleted', caption: 'وضعیت', cellTemplate: function (container, options) {
                badge(container, options.value ? 'حذف شده' : 'فعال', options.value ? 'text-bg-danger' : 'text-bg-success');
            } },
            { caption: 'عملیات', minWidth: 250, cellTemplate: function (container, options) {
                appendActions(container, 'employee', options.data.Id);
            } }
        ],
        attributes: [
            { dataField: 'AttributeCode', caption: 'کد', cssClass: 'mono' },
            { dataField: 'Title', caption: 'عنوان' },
            { dataField: 'DataKindTitle', caption: 'نوع' },
            { dataField: 'SourceKindTitle', caption: 'منبع' },
            { dataField: 'IsRequired', caption: 'الزامی', cellTemplate: function (container, options) {
                $(container).text(options.value ? 'بله' : 'خیر');
            } },
            { dataField: 'DisplayOrder', caption: 'ترتیب', width: 90 },
            { dataField: 'IsDeleted', caption: 'وضعیت', cellTemplate: function (container, options) {
                badge(container, options.value ? 'حذف شده' : 'فعال', options.value ? 'text-bg-danger' : 'text-bg-success');
            } },
            { caption: 'عملیات', minWidth: 290, cellTemplate: function (container, options) {
                appendActions(container, 'attribute', options.data.Id);
            } }
        ],
        search: [
            { dataField: 'Id', caption: 'شناسه', width: 90 },
            { dataField: 'EmployeeCode', caption: 'کد', cssClass: 'mono' },
            { dataField: 'FullName', caption: 'نام و نام خانوادگی' },
            { dataField: 'HasStaleFormula', caption: 'وضعیت فرمول', cellTemplate: function (container, options) {
                badge(container, options.value ? 'نتیجه قدیمی دارد' : 'عادی', options.value ? 'text-bg-warning' : 'text-bg-success');
            } },
            { caption: 'عملیات', width: 130, cellTemplate: function (container, options) {
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
                noDataText: node.attr('data-empty-message') || 'رکوردی یافت نشد.'
            });
        });
    }

    window.TIRDataGrid = { create: create };
    $(mountStaticGrids);
})(jQuery);
