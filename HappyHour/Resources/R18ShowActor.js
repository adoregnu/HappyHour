const INFO_CLASS_NAME ='sc-cQDEqr jBzZAk'

let is_scrolled = false;
let counter = 0;
function scrollToActor() {
    window.scrollTo(0, document.body.scrollHeight / 2);
    is_scrolled = true;
}

(function () {
    // Callback function to execute when mutations are observed
    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            //console.log(mutation.type);
            if (mutation.type != 'childList') {
                return;
            }
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeName == 'DIV' && node.className == INFO_CLASS_NAME) {
                    console.log(node.className)
                    scrollToActor();
                }
                if (node.nodeName != 'IMG') {
                    //console.log(node.nodeName + ', ' + node.className)
                    return;
                }
                //var elm = node.parentElement.parentElement.parentElement;
                //if (elm.className.endsWith('actress-switching')) {
                if (is_scrolled && counter == 5) {
                    //console.log(elm.className)
                    CefSharp.PostMessage("load_completed");
                }
                counter += 1;
                //console.log(counter);
            })
        });
    });

    // Options for the observer (which mutations to observe)
    const config = { attributes: true, childList: true, subtree: true };

    // Start observing the target node for configured mutations
    observer.observe(document.body, config);

    var info = document.getElementsByClassName(INFO_CLASS_NAME);
    if (info.length > 0) {
        scrollToActor();
    }
    // Later, you can stop observing
    //observer.disconnect();
}) ();