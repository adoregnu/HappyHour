(function () {
    const _PID = '{{pid}}';
    function get_node(node) { return node; }

    function parseList(xpath) {
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null) {
            return null;
        }
        var list = [];
        for (var i = 0; i < nodes.length; i++) {
            var item = {}
            var title = _jav_parse_single_node("td[2]/a[starts-with(.,'+++ [FHD]')]", null, nodes[i]);
            if (title != null) {
                item['title'] = title
                item['torrent'] = _jav_parse_single_node("td[3]/a[1]", function (node) { return node.href }, nodes[i]);
                item['magnet'] = _jav_parse_single_node("td[3]/a[2]", function (node) { return node.href }, nodes[i]);

                list.push(item);
            }
        }

        return list;
    }
    function parseLink(xpath) {
        return _jav_parse_single_node(xpath, function (node) { return node.href; });
    }

    var items = {
        torrents: { xpath: "//tr[@class='success']", handler: parseList },
        nextPage: { xpath: "//li[@class='active']/following-sibling::li/a", handler: parseLink }
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