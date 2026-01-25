///*=============== SHOW MENU ===============*/
//const showMenu = (toggleId, navId) => {
//    const toggle = document.getElementById(toggleId),
//        nav = document.getElementById(navId)

//    toggle.addEventListener('click', () => {
//        // Add show-menu class to nav menu
//        nav.classList.toggle('show-menu')

//        // Add show-icon to show and hide the menu icon
//        toggle.classList.toggle('show-icon')
//    })
//}

//showMenu('nav-toggle', 'nav-menu')
function adjustCartMinHeight() {
    const header = document.querySelector('header'); // header elementiniz
    const footer = document.querySelector('footer'); // footer elementiniz
    const container = document.getElementById('cartContainer');

    const headerHeight = header ? header.offsetHeight : 0;
    const footerHeight = footer ? footer.offsetHeight : 0;

    const vh = window.innerHeight;
    const minHeight = vh - headerHeight - footerHeight + 20;

    container.style.minHeight = minHeight + 'px';
}

// Sayfa yüklendiğinde
document.addEventListener('DOMContentLoaded', adjustCartMinHeight);
// Pencere boyutu değiştiğinde
window.addEventListener('resize', adjustCartMinHeight);

document.addEventListener("DOMContentLoaded", function () {
    // HAMBURGER MENÜ
    const burger = document.getElementById("categoryBurger");
    const mobileDropdown = document.getElementById("categoryDropdown");

    burger?.addEventListener("click", function () {
        mobileDropdown.classList.toggle("d-none");
        mobileDropdown.classList.toggle("d-block");
    });


    // MOBİL MENÜ ALT KATEGORİLERİ
    document.querySelectorAll(".submenu-toggle").forEach(function (toggle) {
        toggle.addEventListener("click", function (e) {
            e.stopPropagation(); // dışarıya tıklamayı engelle
            const wrapper = this.closest(".category-menu-item-wrapper");
            wrapper.classList.toggle("open");
        });
    });

    const menuButton = document.getElementById("menu-button");
    const menu = document.getElementById("sortingDiv");

    if (menuButton && menu) {
        // Menü başlangıçta kapalı
        menu.classList.add("hidden");
        menuButton.setAttribute("aria-expanded", "false");

        menuButton.addEventListener("click", function (e) {
            e.preventDefault();
            const isOpen = menu.classList.contains("hidden") === false;
            if (isOpen) {
                menu.classList.add("hidden");
                menuButton.setAttribute("aria-expanded", "false");
            } else {
                menu.classList.remove("hidden");
                menuButton.setAttribute("aria-expanded", "true");
            }
        });

        // Menü dışında bir yere tıklanınca menüyü kapat
        document.addEventListener("click", function (e) {
            if (!menu.contains(e.target) && !menuButton.contains(e.target)) {
                menu.classList.add("hidden");
                menuButton.setAttribute("aria-expanded", "false");
            }
        });
    }

    document.querySelectorAll(".submenu-toggle").forEach(function (toggle) {
        toggle.addEventListener("click", function (e) {
            // üstteki a'ya tıklamayı engelle
            e.stopPropagation();
            e.preventDefault();

            // Burada submenu aç-kapa yapabilirsin:
            const submenu = this.closest(".category-menu-item-wrapper").querySelector(".submenu");
            submenu.classList.toggle("open");
        });
    });

    //document.getElementById("btnSearch").addEventListener("click", function (e) {
    //    // form elemanlarını bul
    //    var form = this.closest("form");
    //    form.querySelectorAll("input, select").forEach(function (el) {
    //        // value boşsa name'i kaldır
    //        if ((el.tagName === "INPUT" || el.tagName === "SELECT") && el.value === "") {
    //            el.disabled = true;
    //        }
    //    });
    //});

    // SEARCH IN ALL PRODUCTS
    const input = document.getElementById("mainSearchInput");
    const icon = document.getElementById("mainSearchIcon");

    icon.addEventListener("click", function () {
        const query = input.value.trim();

        if (query !== "") {
            window.location.href = `/Products/Search?query=${encodeURIComponent(query)}`;
        }
    });

    // Enter
    input.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            icon.click();
        }
    });
});

$('.category-link').hover(function () {
    const targetId = $(this).data('target');
    // önce tüm dropdownları kapat
    $('.mega-dropdown').hide();
    // ilgili dropdownu göster
    $('#' + targetId).show();
});

// mouse header alanından çıkarsa dropdownları kapat
$('.header').mouseleave(function () {
    $('.mega-dropdown').hide();
});
function changeQty(productId, delta) {
    const input = document.querySelector(`input[name='quantity'][value][data-product-id='${productId}']`);
    if (!input) return;

    let value = parseInt(input.value) || 1;
    value += delta;
    if (value < 1) value = 1;
    input.value = value;
}