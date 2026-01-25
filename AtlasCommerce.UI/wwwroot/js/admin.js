function setCookie(name, value, days = 365) {
    const expires = new Date(Date.now() + days * 864e5).toUTCString();
    document.cookie = `${name}=${encodeURIComponent(value)}; path=/; expires=${expires}`;
}

function getCookie(name, defaultValue) {
    const match = document.cookie.match(new RegExp(
        '(?:^|; )' + name.replace(/([.$?*|{}()[\]\\/+^])/g, '\\$1') + '=([^;]*)'
    ));
    return match ? decodeURIComponent(match[1]) : defaultValue;
}

const sidebar = document.getElementById('sidebar');
const switchMode = document.getElementById('switch-mode');

const savedTheme = getCookie('admin-theme', 'light');
const darkOn = savedTheme === 'dark';
document.body.classList.add(savedTheme);
if (switchMode) switchMode.checked = darkOn;

const savedSidebar = getCookie('admin-sidebar', 'open');
const closed = savedSidebar === 'closed';
sidebar.classList.toggle('hide', closed);

switchMode.addEventListener('change', function () {
    if (this.checked) {
        document.body.classList.add('dark');
        setCookie('admin-theme', 'dark');
    } else {
        document.body.classList.remove('dark');
        setCookie('admin-theme', 'light');
    }
})

const menuBar = document.querySelector('#content nav .bx.bx-menu');
if (menuBar) {
    menuBar.addEventListener('click', function () {
        const nowClosed = sidebar.classList.toggle('hide');
        setCookie('admin-sidebar', nowClosed ? 'closed' : 'open');
    });
}

const allSideMenu = document.querySelectorAll('#sidebar .side-menu.top li a');
allSideMenu.forEach(item => {
    const li = item.parentElement;
    item.addEventListener('click', function () {
        allSideMenu.forEach(i => i.parentElement.classList.remove('active'));
        li.classList.add('active');
    });
});

const searchButton = document.querySelector('#content nav form .form-input button');
const searchButtonIcon = document.querySelector('#content nav form .form-input button .bx');
const searchForm = document.querySelector('#content nav form');
if (searchButton) {
    searchButton.addEventListener('click', function (e) {
        if (window.innerWidth < 576) {
            e.preventDefault();
            searchForm.classList.toggle('show');
            if (searchForm.classList.contains('show'))
                searchButtonIcon.classList.replace('bx-search', 'bx-x');
            else
                searchButtonIcon.classList.replace('bx-x', 'bx-search');
        }
    });
    window.addEventListener('resize', function () {
        if (this.innerWidth > 576) {
            searchButtonIcon.classList.replace('bx-x', 'bx-search');
            searchForm.classList.remove('show');
        }
    });
    if (window.innerWidth < 768) {
        sidebar.classList.add('hide');
    } else if (window.innerWidth > 576) {
        searchButtonIcon.classList.replace('bx-x', 'bx-search');
        searchForm.classList.remove('show');
    }
}

function showConfirmationModal(message, onConfirm) {
    $('#confirmModalBody').text(message);
    var myModal = new bootstrap.Modal(document.getElementById('confirmModal'));
    myModal.show();
    $('#confirmModalYesBtn').off('click').on('click', function () {
        if (typeof onConfirm === 'function') onConfirm();
        myModal.hide();
    });
}