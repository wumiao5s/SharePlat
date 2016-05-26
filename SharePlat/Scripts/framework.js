//创建命名空间
$.cnblogs = {};

//总基类
$.cnblogs.Extensible = function(options) {
    options = options || {};
    this.initialize(options);
};

//基类方法
$.extend($.cnblogs.Extensible, {
    //initialize方法用来代替构造函数
    initialize: function(options) {
        $.extend(this, options);
    }
});

//extend方法
//说明:
//  生成当前类的子类
//参数:
//  overrides: 用来重写基类的内容
//附加:
//  生成的子类同样有extend方法可再生成子类
//  生成的子类有baseClass成员表示其父类
//  子类的initialize方法最好先调用父类的initialize方法，调用方法如下
//  子类名.baseClass.initialize.call(this, options);
//示例:
//  var MyClass = Extensible.extend({
//      initialize: function(options) {
//          MyClass.baseClass.initialize.call(this, options);
//          alert(options.msg);
//      }
//  });
$.cnblogs.Extensible.extend = function(overrides) {
    var superClass = this;

    var subClass = function(options) {
        options = options || {};
        $.extend(this, subClass);
        this.initialize(options);
    };

    $.extend(subClass, superClass);
    $.extend(subClass, overrides);

    var p = function() { };
    p.prototype = superClass.prototype;
    subClass.prototype = new p();

    subClass.prototype.constructor = subClass;

    subClass.baseClass = superClass;

    return subClass;
};

//Control类
$.cnblogs.Control = $.cnblogs.Extensible.extend({
    //getContentObj方法
    //说明:
    //  生成并返回此控件html内容的jQuery对象
    //返回:
    //  此控件html内容的jQuery对象
    getContentObj: function() { },

    //cacheResult成员
    //说明:
    //  如果此值为true,则多次show方法的调用只会执行一次getContentObj方法
    //  如果为false则每次show都会调用getContentObj方法
    //  默认为true
    cacheResult: false,

    initialize: function(options) {
        this.container = options.container;
        $.cnblogs.Control.baseClass.initialize.call(this, options);
    },

    //render方法
    //说明:
    //  将此Control对象的内容显示在container中
    //参数:
    //  container: 显示此Control对象的容器
    //返回:
    //  无返回值
    //示例:
    //  var c = new $.cnblogs.Control();
    //  //c.show('#myDiv'); //可以使用ID直接传
    //  c.show($('#myDiv')); //推荐使用jQuery对象
    render: function() {
        var obj;
        if (!this.cacheResult || !this.contentObj) {
            obj = this.getContentObj();
            if (this.cacheResult) {
                this.contentObj = obj;
            }
        }
        else {
            obj = this.contentObj;
        }
        if (typeof this.container == 'string') {
            this.container = $('#' + this.container);
        }
        $(this.container).html('').append(obj);
    }
});

//BindingSource类
$.cnblogs.BindingSource = $.cnblogs.Extensible.extend({
    //clearHtml成员
    //说明:
    //  为true时在绑定前清除被绑定对象的innerHTMl
    clearHtml: true,

    //getDataObj方法
    //说明:
    //  获取每一个绑定数据经处理后的jQuery对象
    //参数:
    //  data: 待绑定的单条数据
    //  index: data在所有数据中的索引
    //返回:
    //  jQuery对象
    //示例:
    //  getDataObj : function(data, index) {
    //      return '#' + index.toString() + ': ' + data.toString();
    //  }
    getDataObj: function(data, index) {
        return data;
    },

    //bindTo方法
    //说明:
    //  将内容绑定到指定的html对象
    //参数:
    //  options: 绑定数据的参数,其中可包括一个或多个待绑定的对象
    //返回:
    //  无返回值
    bindTo: function(options) { }
});

$.cnblogs.AjaxBindingSource = $.cnblogs.BindingSource.extend({
    initialize: function(options) {
        $.cnblogs.AjaxBindingSource.baseClass.initialize.call(this, options); //调用基类

        this.url = options.url;
        this.method = options.method || 'post';
        this.contentType = options.contentType || 'application/json; charset=utf-8';
        this.data = options.data || {};
        this.isAsmx = (options.isAsmx == null) ? true : options.isAsmx;
        this.wrapper = options.wrapper;
        this.callback = options.callback || { scope: this, handler: function(data) { } };

        this.isPending = false;
    },

    prepareData: function(options) {
        var data = this.data;
        if (data) {
            if (this.isAsmx) {
                //如果是调用ASP.NET Web Service,需要将data格式化为json串
                data = JSON.stringify(data);
            }
            return data;
        }
        else {
            return null;
        }
    },

    bindTo: function(options) {
        //如果已经有AJAX请求则不再重复请求
        if (!this.isPending) {
            var target;
            target = options.target;
            if (typeof target == 'string') {
                target = $('#' + options.target);
            }
            var self = this; //保持this引用

            //根据要求清除target的innerHTML值
            if (this.clearHtml) {
                target.html('');
            }

            var data = this.prepareData(options);

            this.isPending = true;

            $.ajax({
                url: this.url || options.url,
                type: this.method,
                data: data,
                contentType: this.contentType,
                dataType: 'json',
                cache: false,

                success: function(data, status) {
                    self.isPending = false;
                    self.callback.handler.call(self.callback.scope, data);
                    if (self.wrapper) {
                        data = data[self.wrapper];
                    }
                    if (data.length) {
                        if (data.length > 1) {
                            for (var i = 0; i < data.length; i++) {
                                var obj = self.getDataObj(data[i], i);
                                target.append(obj);
                            }
                        }
                        else if (data.length == 1) {
                            var obj = self.getDataObj(data[0], 0);
                            target.append(obj);
                        }
                    }
                    else {
                        var obj = self.getDataObj(data[0], 0);
                        target.append(obj);
                    }
                },

                error: function(xhr, status, ex) {
                    self.isPending = false;
                    target.append(xhr.responseText);
                }
            });
        }
    }
});

$.cnblogs.PagedAjaxBindingSource = $.cnblogs.AjaxBindingSource.extend({
    prepareData: function(options) {
        var data = this.data || {};
        var pageIndex = options.pageIndex || 1;
        var pageSize = options.pageSize || 20;
        data['pageIndex'] = pageIndex;
        data['pageSize'] = pageSize;
        if (this.isAsmx) {
            //如果是调用ASP.NET Web Service,需要将data格式化为json串
            data = JSON.stringify(data);
        }
        return data;
    }
});

$.cnblogs.SimpleBindingSource = $.cnblogs.BindingSource.extend({
    initialize: function(options) {
        $.cnblogs.SimpleBindingSource.baseClass.initialize.call(this, options); //调用基类
        if (options.data) {
            this.data = options.data;
        }
        else {
            //初始化绑定数据
            //在子类中使用this.data.push方法添加数据
            this.data = new Array();
            this.initializeData(this.data);
        }
    },

    initializeData: function(data) {

    },

    bindTo: function(options) {
        var target = $('#' + options.target); //获取被绑定对象
        var self = this; //保持对this对象的引用

        //根据要求清除target的innerHTML值
        if (this.clearHtml) {
            target.html('');
        }

        $.each(this.data, function(index) {
            var obj = self.getDataObj(this, index); //this为循环中的元素值
            target.append(obj); //将内容追加到被绑定对象的innerHTML
        });
    }
});

$.cnblogs.Dialog = $.cnblogs.Extensible.extend({
    //getContentObj方法
    //说明:
    //  生成并返回此控件html内容的jQuery对象
    //返回:
    //  此控件html内容的jQuery对象
    getContentObj: function() { },

    //cacheResult成员
    //说明:
    //  如果此值为true,则多次show方法的调用只会执行一次getContentObj方法
    //  如果为false则每次show都会调用getContentObj方法
    //  默认为true
    cacheResult: true,

    isDialogWrapped: false,

    show: function() {
        if (!this.cacheResult || !this.contentObj) {
            this.contentObj = this.getContentObj();
        }

        if (this.isDialogWrapped) {
            this.contentObj.dialog('open');
        }
        else {
            this.isDialogWrapped = true;
            this.contentObj.dialog(this.dialogOptions);
        }
    },

    close: function(closing) {
        if (this.cacheResult) {
            if (!closing) {
                this.contentObj.dialog('close');
            }
        }
        else {
            this.contentObj.dialog('destroy').remove();
            this.isDialogWrapped = false;
            this.contentObj = null;
        }
    },

    initialize: function(options) {
        var self = this; //保持this引用

        this.dialogOptions = options.dialogOptions || {};
        if (options.cacheResult != null) {
            this.cacheResult = options.cacheResult;
        }
        $.extend(this.dialogOptions, {
            bgiframe: true,
            close: function() {
                self.close(true);
            }
        });
    }
});

$.cnblogs.GenericDialog = $.cnblogs.Dialog.extend({
    initialize: function(options) {
        $.cnblogs.GenericDialog.baseClass.initialize.call(this, options);
        this.getContentObj = options.getContentObj || function() { };
    }
});

//LocationList类
$.cnblogs.LocationList = $.cnblogs.BindingSource.extend({
    initialize: function(options) {
        $.cnblogs.LocationList.baseClass.initialize.call(this, options);
        this.data = [{ name: "北京", cities: ["西城", "东城", "崇文", "宣武", "朝阳", "海淀", "丰台", "石景山", "门头沟", "房山", "通州", "顺义", "大兴", "昌平", "平谷", "怀柔", "密云", "延庆"] }, { name: "天津", cities: ["青羊", "河东", "河西", "南开", "河北", "红桥", "塘沽", "汉沽", "大港", "东丽", "西青", "北辰", "津南", "武清", "宝坻", "静海", "宁河", "蓟县", "开发区"] }, { name: "河北", cities: ["石家庄", "秦皇岛", "廊坊", "保定", "邯郸", "唐山", "邢台", "衡水", "张家口", "承德", "沧州", "衡水"] }, { name: "山西", cities: ["太原", "大同", "长治", "晋中", "阳泉", "朔州", "运城", "临汾"] }, { name: "内蒙古", cities: ["呼和浩特", "赤峰", "通辽", "锡林郭勒", "兴安"] }, { name: "辽宁", cities: ["大连", "沈阳", "鞍山", "抚顺", "营口", "锦州", "丹东", "朝阳", "辽阳", "阜新", "铁岭", "盘锦", "本溪", "葫芦岛"] }, { name: "吉林", cities: ["长春", "吉林", "四平", "辽源", "通化", "延吉", "白城", "辽源", "松原", "临江", "珲春"] }, { name: "黑龙江", cities: ["哈尔滨", "齐齐哈尔", "大庆", "牡丹江", "鹤岗", "佳木斯", "绥化"] }, { name: "上海", cities: ["浦东", "杨浦", "徐汇", "静安", "卢湾", "黄浦", "普陀", "闸北", "虹口", "长宁", "宝山", "闵行", "嘉定", "金山", "松江", "青浦", "崇明", "奉贤", "南汇"] }, { name: "江苏", cities: ["南京", "苏州", "无锡", "常州", "扬州", "徐州", "南通", "镇江", "泰州", "淮安", "连云港", "宿迁", "盐城", "淮阴", "沐阳", "张家港"] }, { name: "浙江", cities: ["杭州", "金华", "宁波", "温州", "嘉兴", "绍兴", "丽水", "湖州", "台州", "舟山", "衢州"] }, { name: "安徽", cities: ["合肥", "马鞍山", "蚌埠", "黄山", "芜湖", "淮南", "铜陵", "阜阳", "宣城", "安庆"] }, { name: "福建", cities: ["福州", "厦门", "泉州", "漳州", "南平", "龙岩", "莆田", "三明", "宁德"] }, { name: "江西", cities: ["南昌", "景德镇", "上饶", "萍乡", "九江", "吉安", "宜春", "鹰潭", "新余", "赣州"] }, { name: "山东", cities: ["青岛", "济南", "淄博", "烟台", "泰安", "临沂", "日照", "德州", "威海", "东营", "荷泽", "济宁", "潍坊", "枣庄", "聊城"] }, { name: "河南", cities: ["郑州", "洛阳", "开封", "平顶山", "濮阳", "安阳", "许昌", "南阳", "信阳", "周口", "新乡", "焦作", "三门峡", "商丘"] }, { name: "湖北", cities: ["武汉", "襄樊", "孝感", "十堰", "荆州", "黄石", "宜昌", "黄冈", "恩施", "鄂州", "江汉", "随枣", "荆沙", "咸宁"] }, { name: "湖南", cities: ["长沙", "湘潭", "岳阳", "株洲", "怀化", "永州", "益阳", "张家界", "常德", "衡阳", "湘西", "邵阳", "娄底", "郴州"] }, { name: "广东", cities: ["广州", "深圳", "东莞", "佛山", "珠海", "汕头", "韶关", "江门", "梅州", "揭阳", "中山", "河源", "惠州", "茂名", "湛江", "阳江", "潮州", "云浮", "汕尾", "潮阳", "肇庆", "顺德", "清远"] }, { name: "广西", cities: ["南宁", "桂林", "柳州", "梧州", "来宾", "贵港", "玉林", "贺州"] }, { name: "海南", cities: ["海口", "三亚"] }, { name: "重庆", cities: ["渝中", "大渡口", "江北", "沙坪坝", "九龙坡", "南岸", "北碚", "万盛", "双桥", "渝北", "巴南", "万州", "涪陵", "黔江", "长寿"] }, { name: "四川", cities: ["成都", "达州", "南充", "乐山", "绵阳", "德阳", "内江", "遂宁", "宜宾", "巴中", "自贡", "康定", "攀枝花"] }, { name: "贵州", cities: ["贵阳", "遵义", "安顺", "黔西南", "都匀"] }, { name: "云南", cities: ["昆明", "丽江", "昭通", "玉溪", "临沧", "文山", "红河", "楚雄", "大理"] }, { name: "西藏", cities: ["拉萨", "林芝", "日喀则", "昌都"] }, { name: "陕西", cities: ["西安", "咸阳", "延安", "汉中", "榆林", "商南", "略阳", "宜君", "麟游", "白河"] }, { name: "甘肃", cities: ["兰州", "金昌", "天水", "武威", "张掖", "平凉", "酒泉"] }, { name: "青海", cities: ["黄南", "海南", "西宁", "海东", "海西", "海北", "果洛", "玉树"] }, { name: "宁夏", cities: ["银川", "吴忠"] }, { name: "新疆", cities: ["乌鲁木齐", "哈密", "喀什", "巴音郭楞", "昌吉", "伊犁", "阿勒泰", "克拉玛依", "博尔塔拉"]}]
    },

    getOptionHtml: function(value) {
        return '<option value="' + value + '">' + value + '</option>';
    },

    getProvinceHtml: function() {
        var html = '';
        for (var i = 0; i < this.data.length; i++) {
            html += this.getOptionHtml(this.data[i].name);
        }
        return html;
    },

    getCityHtml: function(province) {
        var html = '<option value="'+province+'">' + province + '</option>';
        var cities = this.getCities(province);
        for (var i = 0; i < cities.length; i++) {
            html += this.getOptionHtml(cities[i]);
        }
        return html;
    },

    getCities: function(province) {
        for (var i = 0; i < this.data.length; i++) {
            if (this.data[i].name == province) {
                return this.data[i].cities;
            }
        }
        return new Array();
    },

    bindTo: function(options) {
        options = options || {};
        this.pList = $('#' + options.pList);
        this.cList = $('#' + options.cList);

        this.pList.html('')
            .append($(this.getOptionHtml(options.defaultProvince)))
            .append($(this.getProvinceHtml()));

        var self = this;

        this.pList.bind('change', function(event) {
            var province = this.value;
            var cityObj = $(self.getCityHtml(province));
            self.cList.html('').append(cityObj);
        });

        this.select(options.selected);
    },

    select: function(value) {
        if (value && value.province) {
            this.pList.attr('value', value.province).change();
            this.cList.attr('value', value.city);
        }
    }
});

//debugger;

$.create = function(options, parent) {
    if (!options || !options.tag) {
        return;
    }
    var element;
    if (options.tag.toLowerCase() == 'input') {
        element = '<input type="' + options.type + '" />';
    }
    else {
        element = '<' + options.tag + ' />';
    }
    element = $(element);
    if (parent) {
        parent.append(element);
    }
    if (typeof options.attributes == 'object') {
        element.attr(options.attributes);
    }
    if (typeof options.style == 'object') {
        element.css(options.style);
    }
    if (typeof options.className == 'string') {
        element.addClass(options.className);
    }
    if (typeof options.text == 'string') {
        element.html(options.text);
    }
    if (typeof options.value == 'string') {
        element.val(options.value);
    }
    if (options.callback) {
        options.callback(element);
    }
    if (options.children) {
        if (options.children.length) {
            for (var i = 0; i < options.children.length; i++) {
                var child = $.create(options.children[i], element);
            }
        }
        else {
            var child = $.create(options.children);
            element.append(child);
        }
    }
    return element;
};

$.cnblogs.DoubleListBindingSource = $.cnblogs.BindingSource.extend({
    initialize: function(options) {
        $.cnblogs.DoubleListBindingSource.baseClass.initialize.call(this, options);
        this.data = options.data || new Array();
    },

    bindTo: function(options) {
        var self = this; //保持this引用

        var parent = options.parent;
        var child = options.child;
        if (typeof parent == 'string') {
            parent = $('#' + parent);
        }
        if (typeof child == 'string') {
            child = $('#' + child);
        }
        var defaultItem = options.defaultItem;

        //填充父列表
        if (defaultItem) {
            parent.append(
                $.create({ tag: 'option', value: defaultItem.value, text: defaultItem.text })
            );
            child.append(
                $.create({ tag: 'option', value: defaultItem.value, text: defaultItem.text })
            );
        }
        $.each(this.data, function() {
            parent.append(
                $.create({ tag: 'option', value: this.value, text: this.text })
            );
        });
        //绑定父列表事件
        parent.change(function() {
            //值有效，绑定子列表
            child.html('');
            if (defaultItem) {
                child.append(
                    $.create({ tag: 'option', value: defaultItem.value, text: defaultItem.text })
                );
            }
            var value = parent.val();
            if (!defaultItem || value != defaultItem.value) {
                var subArr = $.grep(self.data, function(item) { return (item.value == value); });
                if (subArr.length > 0) {
                    var items = subArr[0].items;
                    $.each(items, function() {
                        child.append(
                            $.create({ tag: 'option', value: this.value, text: this.text })
                        );
                    });
                }
            }
        });
    }
});