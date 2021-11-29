﻿const _PID = '{{pid}}';
const _USERID = '{{userid}}';
const _PASSWORD = '{{password}}';

function _parseSingleNode(_xpath, _getter = null) {
    var result = document.evaluate(_xpath, document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var node = result.singleNodeValue;
    if (node != null) {
        if (_getter != null) {
            return _getter(node);
        } else {
            return node.textContent.trim();
        }
    }
    return null;
}

function _checkLogin() {
    var node = _parseSingleNode("//form[@id='sidebar_login_form']", function (n) {
        return n;
    });
    if (node != null && _USERID.length > 0 && _PASSWORD.length > 0) {
        document.querySelector("div.sidebar-login input[name='mb_id']").value = _USERID;
        document.querySelector("div.sidebar-login input[name='mb_password']").value = _PASSWORD;
        document.querySelector("div.sidebar-login button").click();
        return false;
    }
    return true;
}

function _checkSearchResult() {
    const xpath = "//div[@class='media-body']/a[" +
        "contains(@href, 'table=jamak') or " +
        "contains(@href,'table=bigsub')]";
    var url = _parseSingleNode(xpath, function (n) { return n; });
    if (url != null) {
        CefSharp.PostMessage({ type: 'url', data: url.href});
        return true;
    }
    return false;
}

(function () {

    if (!_checkLogin()) {
        return;
    }

    if (_checkSearchResult()) {
        return;
    }

    var items = { };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _parseSingleNode(item['xpath']);
        } else {
            msg[key] = item['handler'](item['xpath']);
        }
        if (msg[key] == null) {
            continue;
        }
        //console.log(key + ': ' + msg[key]);
        num_item += 1;
    }
    msg['data'] = num_item;
    CefSharp.PostMessage(msg);
}) ();