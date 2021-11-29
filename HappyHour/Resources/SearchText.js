(function () {
    var text = window.getSelection().toString();
    console.log(text);
    if (text.length > 0) {
        var message = {
            type: "text",
            data: text,
            action: "{{action}}",
            spider: "{{spider}}"
        };
        CefSharp.PostMessage(message);
    }
    return text;
})();
