/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: 'class',              // Tema deðiþimini class bazlý kontrol et
    content: [
        './**/*.cshtml',              // Razor sayfalarýnýzý tarar
        './wwwroot/js/**/*.js',       // JS içinde Tailwind sýnýflarý arar
        './css/**/*.css'              // Özel CSS dosyalarýnýzý tarar
    ],
    theme: {
        extend: {},
    },
    plugins: [],
}