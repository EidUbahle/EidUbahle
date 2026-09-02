window.Wamo = (function ($) {
    'use strict';
    var deferredPrompt = null;
    function meta(name) { return $('meta[name="' + name + '"]').attr('content') || ''; }
    function ajax(pageMethod, data, options) {
        options = options || {};
        return $.ajax({ url: pageMethod, type: 'POST', contentType: 'application/json; charset=utf-8', dataType: 'json', headers: { 'X-CSRF-Token': meta('csrf-token') }, data: JSON.stringify(data || {}), success: function (response) { if (options.success) options.success(response.d || response); }, error: function (xhr) { if (window.toastr) toastr.error('Something went wrong. Please try again.'); if (options.error) options.error(xhr); } });
    }
    function setTheme(theme) { $('body').removeClass('theme-green theme-blue theme-dark theme-light').addClass(theme); localStorage.setItem('wamo-theme', theme); $('#themeSwitcher').val(theme); }
    function initTheme() { setTheme(localStorage.getItem('wamo-theme') || 'theme-green'); $('#themeSwitcher').on('change', function () { setTheme($(this).val()); }); }
    function initPwa() {
        if ('serviceWorker' in navigator) navigator.serviceWorker.register('/PWA/service-worker.js').catch(function () {});
        window.addEventListener('beforeinstallprompt', function (event) { event.preventDefault(); deferredPrompt = event; $('#pwaInstallPrompt').removeClass('d-none'); });
        var isiOS = /iphone|ipad|ipod/i.test(window.navigator.userAgent); var standalone = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone;
        if (isiOS && !standalone) { $('#pwaInstallPrompt').removeClass('d-none'); $('#iosInstallHelp').removeClass('d-none'); $('#btnInstallPwa').addClass('d-none'); }
        $('#btnInstallPwa').on('click', function () { if (!deferredPrompt) return; deferredPrompt.prompt(); deferredPrompt.userChoice.finally(function () { deferredPrompt = null; $('#pwaInstallPrompt').addClass('d-none'); }); });
        $('#btnLaterPwa').on('click', function () { $('#pwaInstallPrompt').addClass('d-none'); });
    }
    function fillSelect(selector, data, valueField, textField) { var html = '<option value="">Select</option>'; $.each(data || [], function (_, item) { html += '<option value="' + item[valueField] + '">' + item[textField] + '</option>'; }); $(selector).html(html); }
    function formJson($form) { var data = {}; $.each($form.serializeArray(), function (_, pair) { data[pair.name] = pair.value; }); return data; }
    function renderCards(selector, items, renderer) { var host = $(selector).empty(); if (!items || !items.length) { host.html('<div class="col-12"><div class="alert alert-light border">No data available.</div></div>'); return; } $.each(items, function (_, item) { host.append(renderer(item)); }); }
    function initLanguages() { ajax('/Default.aspx/GetLanguages', {}, { success: function (result) { var data = result.data || result; var current = meta('current-language'); var html = ''; $.each(data, function (_, language) { html += '<option value="' + language.LanguageCode + '"' + (language.LanguageCode === current ? ' selected' : '') + '>' + language.NativeName + '</option>'; }); $('#languageSwitcher').html(html).on('change', function () { window.location.search = '?lang=' + encodeURIComponent($(this).val()); }); } }); }
    function initAdminTable() { var table = $('#adminDataTable'); if (!table.length) return; ajax(window.location.pathname + '/GetList', {}, { success: function (payload) { var rows = payload.data || []; if (!rows.length) { table.html('<thead><tr><th>Info</th></tr></thead><tbody><tr><td>No records found.</td></tr></tbody>'); return; } var columns = Object.keys(rows[0]); var thead = '<thead><tr>' + $.map(columns, function (c) { return '<th>' + c + '</th>'; }).join('') + '</tr></thead>'; var tbody = '<tbody>' + $.map(rows, function (row) { return '<tr>' + $.map(columns, function (c) { return '<td>' + (row[c] == null ? '' : row[c]) + '</td>'; }).join('') + '</tr>'; }).join('') + '</tbody>'; table.html(thead + tbody).DataTable({ responsive: true, pageLength: 10 }); } }); }
    function initPublic() {
        if ($('#announcementsList').length) ajax('/Default.aspx/GetAnnouncements', {}, { success: function (result) { renderCards('#announcementsList', result.data, function (item) { return '<div class="col-md-6 col-xl-4"><div class="card h-100 shadow-sm"><div class="card-body"><h5>' + item.Title + '</h5><p class="text-muted small">' + item.StartDate + '</p><p>' + item.Content + '</p></div></div></div>'; }); } });
        if ($('#blogPosts').length) ajax('/Blog.aspx/GetPosts', {}, { success: function (result) { renderCards('#blogPosts', result.data, function (item) { return '<div class="col-md-6"><div class="card h-100 shadow-sm"><div class="card-body"><span class="badge bg-success mb-2">' + item.CategoryName + '</span><h5>' + item.Title + '</h5><p>' + item.Summary + '</p></div></div></div>'; }); } });
        if ($('#contactCards').length) ajax('/Contact.aspx/GetContactDetails', {}, { success: function (result) { renderCards('#contactCards', result.data, function (item) { return '<div class="col-md-4"><a class="card h-100 shadow-sm text-decoration-none" target="_blank" href="' + item.Url + '"><div class="card-body"><h5>' + item.Label + '</h5><p>' + item.Value + '</p></div></a></div>'; }); } });
        if ($('#accountDashboard').length) ajax('/Account.aspx/GetAccountSummary', {}, { success: function (result) { $('#accountDashboard').html('<pre class="small mb-0">' + JSON.stringify(result.data, null, 2) + '</pre>'); } });
        if ($('#registerCustomerForm').length) {
            ajax('/Register.aspx/GetSections', {}, { success: function (result) { fillSelect('#SectionID', result.data, 'SectionID', 'SectionName'); } });
            $('#SectionID').on('change', function () { ajax('/Register.aspx/GetBlocks', { sectionId: parseInt($(this).val() || '0', 10) }, { success: function (result) { fillSelect('#BlockID', result.data, 'BlockID', 'BlockNumber'); $('#HouseID').html('<option value="">Select house</option>'); } }); });
            $('#BlockID').on('change', function () { ajax('/Register.aspx/GetHouses', { blockId: parseInt($(this).val() || '0', 10) }, { success: function (result) { fillSelect('#HouseID', result.data, 'HouseID', 'HouseNumber'); } }); });
            $('#registerCustomerForm').on('submit', function (e) { e.preventDefault(); ajax('/Register.aspx/RegisterCustomer', formJson($(this)), { success: function (result) { toastr.success(result.message || 'Registration submitted successfully.'); $('#registerCustomerForm')[0].reset(); } }); });
        }
        if ($('#collectionRequestForm').length) $('#collectionRequestForm').on('submit', function (e) { e.preventDefault(); ajax('/Request.aspx/SubmitRequest', formJson($(this)), { success: function (result) { toastr.success(result.message || 'Request submitted.'); } }); });
        if ($('#shopProducts').length) { ajax('/Shop.aspx/GetProducts', {}, { success: function (result) { renderCards('#shopProducts', result.data, function (item) { return '<div class="col-md-6 col-xl-3"><div class="card h-100 shadow-sm"><div class="card-body"><h5>' + item.ProductName + '</h5><p>' + item.Description + '</p><p class="fw-bold">$' + item.Price + '</p><button class="btn btn-success btn-sm btn-whatsapp-order" data-product="' + item.ProductName + '">Order via WhatsApp</button></div></div></div>'; }); } }); $(document).on('click', '.btn-whatsapp-order', function () { var product = $(this).data('product'); var name = $('body').data('current-user') || 'Guest'; var message = encodeURIComponent('Product: ' + product + '
Quantity: 1
Customer Name: ' + name + '
Customer Number: Pending'); window.open('https://wa.me/?text=' + message, '_blank'); }); }
    }
    $(function () { initTheme(); initPwa(); initLanguages(); initPublic(); initAdminTable(); });
    return { ajax: ajax, setTheme: setTheme };
})(jQuery);
