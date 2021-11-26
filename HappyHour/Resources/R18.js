const _PID = '{{pid}}';
const INFO_CLASS_NAME = 'sc-cQDEqr jBzZAk'

function _parseSingleNode(_xpath) {
    var result = document.evaluate(_xpath, document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var node = result.singleNodeValue;
    if (node != null) {
        return node.textContent.trim();
    }
    return null;
}

function _parseMultiNode(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        array.push(node.textContent.trim());
    }
    return array;
}

function _parseActorThumb(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    var reName = new RegExp('([\w\s]+)(?:\s)\((.+)\)', 'i');
    var reAlias = new RegExp('([\w\s]+),?', 'ig');
    while (node = result.iterateNext()) {
        var actorInfo = {};
        var m = reName.exec(node.alt);
        if (m == null) {
            actorInfo['name'] = node.alt;
        } else {
            actorInfo['name'] = m[1];
            var arr;
            var alias = [];
            while ((arr = reAlias.exec(m[2])) !== null) {
                alias.push(arr[1]);
            }
            if (alias.length > 0) {
                actorInfo['alias'] = alias;
            }
        }
        actorInfo['thumb'] = node.src;
        array.push(actorInfo);
    }
    return array;
}

let is_scrolled = false;

function _scrollToActor() {
    window.scrollTo(0, document.body.scrollHeight / 2);
    is_scrolled = true;
}

function _parsePage() {
    var items = {
        title: { xpath: "//meta[@property='og:title']/@content" },
        date: { xpath: "//h3[contains(.,'Release date')]/following-sibling::div//text()" },
        //runtime: { xpath: "//h3[contains(.,'Runtime')]/following-sibling::div/text()" },
        //director: { xpath: "//h3[contains(.,'Director')]/following-sibling::div/text()" },
        series: { xpath: "//h3[contains(.,'Series')]/following-sibling::div//a/text()" },
        studio: { xpath: "//h3[contains(.,'Studio')]/following-sibling::div/a/text()" },
        genre: {
            xpath: "//h3[contains(.,'Categories')]/following-sibling::div/span//text()",
            handler: _parseMultiNode
        },
        plot: { xpath: "//h3[contains(.,'synosis')]/following-sibling::p/text()" },
        cover: { xpath: "//div[@class='sc-cTJmaU BaBOz']/img/@src" },
        cover2: { xpath: "//div[@class='sc-cTJmaU bAAnmR']/img/@src" },
        actor: {
            xpath: "//div[contains(@class,'actress-switching')]//img",
            handler: _parseActorThumb
        },
    };

    var msg = { type : 'items'}
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (key == 'cover2') key = 'cover';
        if (msg[key] != null) continue;

        if (item['handler'] == null) {
            msg[key] = _parseSingleNode(item['xpath']);
        } else {
            msg[key] = item['handler'](item['xpath']);
        }
        num_item += 1;
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}

function _multiResult() {
    var pattern = '/id=(h_)?(\d+)?';
    if (_PID.includes('-')) {
        var tmp = _PID.split('-');
        pattern += tmp[0] + '\d*' + tmp[1];
    } else {
        pattern += _PID;
    }
    pattern += '/i';

    const re = new RegExp(pattern);
    var result = document.evaluate("//li[starts-with(@class,'item-list')]/a",
        document.body, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);
    var num_result = 0;
    while (node = result.iterateNext()) {
        num_result += 1;
        if (re.test(node.href) !== null) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return 'redirected';
        }
    }
    if (num_result > 0) {
        return 'ambiguous';
    } else
        return 'notfound';
}

(function () {

    if (_multiResult() != 'notfound') {
        return;
    }
    var num_loaded = 0;

    // Callback function to execute when mutations are observed
    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            //console.log(mutation.type);
            if (mutation.type != 'childList') {
                return;
            }
            mutation.addedNodes.forEach(function (node) {
                //console.log(node.nodeName + ' class=' + node.className);
                if (node.nodeName == 'DIV' && node.className == INFO_CLASS_NAME) {
                    console.log(node.className)
                    _scrollToActor();
                }
            })
            //console.log('addedNodes:' + mutation.addedNodes.length);
            if (is_scrolled && num_loaded == 5) {
                _parsePage();
            }
            num_loaded += 1;
        });
    });

    // Options for the observer (which mutations to observe)
    const config = { attributes: true, childList: true, subtree: true };

    // Start observing the target node for configured mutations
    observer.observe(document.body, config);

    var info = document.getElementsByClassName(INFO_CLASS_NAME);
    if (info.length > 0) {
        _scrollToActor();
    }
    // Later, you can stop observing
    //observer.disconnect();
}) ();