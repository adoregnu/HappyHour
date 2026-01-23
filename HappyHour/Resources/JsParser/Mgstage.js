(function () {
    function _parseRating(xpath) {
        var txt = _jav_parse_single_node(xpath);
        if (txt != null) {
            var re = new RegExp('[0-9.]+');
            var m = re.exec(txt);
            if (m != null) {
                return m[0];
            }
        }
        return null;
    }

    function _parse_genre(xpath) {
        var genres = _jav_parse_multi_node(xpath);
        if (genres == null || genres.length < 1) {
            return null;
        }
        var drops = ['独占配信', '配信専用', 'フルハイビジョン' ];
        genres = genres.filter(genre => !drops.some(drop => genre.includes(drop)));
        if (genres.length < 1) {
            return null;
        }
        return genres;
    }

    function get_node(n) { return n; }
    function _parse_intro(xpath) {
        var nodes = _jav_parse_multi_node(xpath, get_node);
        if (nodes == null || nodes.length < 1) {
            return null;
        }
        var intro = '';
        for (var i = 0; i < nodes.length; i++) {
            // skip class is 'more'
            if (nodes[i].classList.contains('more')) {
                continue;
            }
            // skip empty text
            if (nodes[i].textContent.trim().length < 1) {
                continue;
            }
            intro += nodes[i].textContent.trim();
        }
        return intro;
    }

    var items = {
        //id: { xpath: "//th[contains(., '品番：')]/following-sibling::td" },
        title: { xpath: "//div[@class='common_detail_cover']/h1[@class='tag']" },
        cover: { xpath: "//a[@id='EnlargeImage']/@href" },
        maker: { xpath: "//th[contains(., 'メーカー：')]/following-sibling::td/a" },
        label: { xpath: "//th[contains(., 'レーベル：')]/following-sibling::td/a" },
        series: { xpath: "//th[contains(., 'シリーズ：')]/following-sibling::td/a" },
        date: { xpath: "//th[contains(., '配信開始日：')]/following-sibling::td" },
        genre: {
            xpath: "//th[contains(., 'ジャンル：')]/following-sibling::td/a/text()",
            handler:  _parse_genre
        },
        rating: {
            xpath: "//th[contains(., '評価：')]/following-sibling::td",
            handler: _parseRating
        },
        plot: {
            xpath: "//*[@id='introduction']/dd/p",
            handler: _parse_intro
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
    _post_message(msg, 'jp');
}) ();