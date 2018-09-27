import 'bootstrap';
import 'jquery-mousewheel';
import 'malihu-custom-scrollbar-plugin/jquery.mCustomScrollbar.js';

$(function(){
    $('#sidebar-collapse').on('click', function () {
        $('body').toggleClass('sidebar-open');
    });
    $("#scrolled-sidebar-content").mCustomScrollbar({
        theme: "dark-3",
        scrollInertia: 0
    });
});

window.$ = $;
window.jQuery = $;