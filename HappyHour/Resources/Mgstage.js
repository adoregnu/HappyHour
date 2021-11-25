const _PID = '{{pid}}';

function _parseSingleNode(_xpath) {
    var result = document.evaluate(_xpath, document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var node = result.singleNodeValue;
    if (node != null) {
        return node.textContent.trim();
    }
    return null;
}

(function () {
    var items = {
        id: { xpath: "//th[contains(., '品番：')]/following-sibling::td" },
        title: { xpath: "//div[@class='common_detail_cover']/h1[@class='tag']" },
        cover: { xpath: "//a[@id='EnlargeImage']/@href" },
        studio: { xpath: "//th[contains(., 'メーカー：')]/following-sibling::td/a" },
        date: { xpath: "//th[contains(., '配信開始日：')]/following-sibling::td" },
        rating: { xpath: "//th[contains(., '評価：')]/following-sibling::td//text()" },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null)
            msg[key] = _parseSingleNode(item['xpath']);
        else
            msg[key] = item['handler'](item['xpath']);
        //console.log(key + ': ' + msg[key]);
        num_item += 1;
    }
    msg['data'] = num_item;
    CefSharp.PostMessage(msg);
}) ();