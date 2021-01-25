(function () {
    var text = window.getSelection().toString();
    /*
    console.log(text);
    if (text.length > 0) {
        var message = {
            "Type" : "SearchText",
            "Data" : text
        };
        CefSharp.PostMessage(message);
    } */
    return text;
})();
