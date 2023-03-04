(function () {
    var parent = document.querySelector('#hdr_srch_mobile');
    //console.log(parent);
    if (parent != null) {
        console.log('{{pid}}');
        parent.querySelector("input").value = '{{pid}}';
        parent.querySelector("button").click();
    }
})();