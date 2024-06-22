(function () {
    const _PID = '{{pid}}';
    function parseActor(xpath) {
        var names = _jav_parse_multi_node(xpath);
        if (names == null) { return null; }
        var array = [];
        for (var i = 0; i < names.length; i++) {
            array.push({ name: names[i] });
        }
        if (array.length > 0) {
            return array;
        }
        return null;
    }

    var items = {
        //id: { xpath: "//th[contains(., '品番：')]/following-sibling::td" },
        title: { xpath: "//h1[@class='h1--dense']" },
        cover: { xpath: "//video[@class='vjs-tech']/@poster" },
        //studio: { xpath: "//th[contains(., 'メーカー：')]/following-sibling::td/a" },
        date: { xpath: "//span[contains(.,'Release Date:')]/following-sibling::span" },
        actor: {
            xpath: "//span[contains(.,'Featuring:')]/following-sibling::span/a",
            handler: parseActor
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _jav_parse_single_node(item['xpath']);
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
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}) ();