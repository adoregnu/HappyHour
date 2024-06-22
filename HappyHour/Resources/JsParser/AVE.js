(function () {
    const _PID = '{{pid}}';
    function _parseActor(xpath) {
        var array = _jav_parse_multi_node(xpath);
        if (array == null) {
            return null;
        }

        var actors = [];
        array.forEach(function (a) {
            actors.push({ name: a });
        })
        if (actors.length > 0) {
            return actors;
        }
        return null;
    }

    function _parseDate(xpath) {
        var txt = _jav_parse_single_node(xpath);
        if (txt != null) {
            var m = /[\d/]+/i.exec(txt);
            if (m != null) {
                var tmp = m[0].replace(/(\d+)/ig, function (d) {
                    if (d.length == 1) {
                        return d.padStart(2, '0');
                    }
                    return d;
                })
                //console.log(m[0] + ', ' + tmp);
                return tmp.replace(/(\d+)\/(\d+)\/(\d+)/i, '$3-$1-$2');
            }
        }
        return null;
    }

    function parseSearchResult() {
        var result = document.evaluate("//div[contains(@class, 'shop-product-wrap')]//p[@class='product-title']/a",
            document.body, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
        if (result.snapshotLength == 1) {
            var node = result.snapshotItem(0);
            CefSharp.PostMessage({ type: 'url', data: node.href });
        } else if (result.snapshotLength > 1) {
            console.log('ambiguous result!');
        }
        else {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        }
    }

    if (document.location.href.includes('/search_Products.aspx')) {
        parseSearchResult()
        return;
    }

    var items = {
        cover: { xpath: "//span[@class='grid-gallery']/a/@href" },
        title: { xpath: "//div[@class='section-title']/h3/text()" },
        actor: {
            xpath: "//span[contains(.,'Starring')]/following-sibling::span//text()",
            handler: _parseActor
        },
        studio: {
            xpath: "//span[contains(.,'Studio')]/following-sibling::span//text()",
        },
        genre: {
            xpath: "//span[contains(., 'Category')]/following-sibling::span/a/text()",
            handler: _jav_parse_multi_node
        },
        series: {
            xpath: "//span[contains(., 'Series')]/following-sibling::span//text()"
        },
        date: {
            xpath: "//div[@class='single-info']/span[contains(.,'Date')]/following-sibling::span//text()",
            handler: _parseDate
        }
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