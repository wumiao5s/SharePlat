$.cnblogs.validation = {}; //创建命名空间

//validator管理器
//用于注册validator的rule或通过rule获取validator对象
$.cnblogs.validation.ValidatorMgr = {
    registerRule: function(rule, type) {
        this[rule] = type;
    },

    createValidator: function(rule, options) {
        options = options || {};
        if (this[rule]) {
            return new this[rule](options);
        }
        return null;
    }
};

//ValidateResult类
//说明:
//  表示验证结果
$.cnblogs.validation.ValidateResult = function() {
    this.result = false;
    this.msg = '';
    this.modifier = null;

    this.onModified = function(result) {
        //result is of type jQuery.ValidateResult
    };

    this.modify = function(result, msg, modifier) {
        this.result = result;
        this.msg = msg;
        this.modifier = modifier;

        if (jQuery.isFunction(this.onModified)) {
            this.onModified(this);
        }
    };
};

//validator基类
//也可通过重写doValidate方法执作自定义validator使用
$.cnblogs.validation.CustomValidator = $.cnblogs.Extensible.extend({
    //验证未通过时显示的消息
    errorMsg: '',

    //下一个validator
    successor: null,

    //可验证的input类型
    acceptTypes: ['text', 'password', 'checkbox', 'radio', 'select'],

    initialize: function(options) {
        $.extend(this, options);
    },

    //可重写的验证方法
    doValidate: function(input, result) {
        this.setError(result);
    },

    //原则上不重写，模板方法
    validate: function(input, result) {
        //判断input的type是否在acceptTypes中，存在则调用验证方法
        //对于select之类的要判断tagName
        if ($.inArray($(input).prop('type').toLowerCase(), this.acceptTypes) != -1 ||
            $.inArray($(input).prop('tagName').toLowerCase(), this.acceptTypes) != -1) {
            //验证
            this.doValidate(input, result);
        }


        //如果验证通过，并且还有下一个validator，交给下一个validator
        if (result.result && this.successor) {
            this.successor.validate(input, result);
        }
    },

    //通知验证错误
    setError: function(result) {
        result.modify(false, this.errorMsg, this);
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('custom', jQuery.CustomValidator); //注册规则custom

//FormValidator类
$.cnblogs.validation.FormValidator = function(form, options) {
    jQuery.extend(this, {
        validators: new Array(),

        addValidator: function(validator) {
            this.validators.push(validator);
        },

        success: function() {
        },

        failure: function(errors) {
        },

        validate: function() {
            var errors = new Array();
            var result = true;

            var formValidator = $(this).data('validator');
            //使用所有的validator进行检验
            $.each(formValidator.validators, function() {
                if (!this.isValid()) {
                    result = false;
                    if (this.result.msg) {
                        errors.push(this.result.msg);
                    }
                }
            });

            //触发事件
            if (result && formValidator.success) {
                formValidator.success();
            }
            else if (formValidator.failure) {
                formValidator.failure(errors);
            }

            return result;
        }
    });

    options = options || {};
    $.extend(this, options);

    //Add event to form
    $(form).bind('submit', this.validate);
};

//formValidator方法
//说明:
//  为form元素绑定相应的验证事件
//参数:
//  options: 生成$.cnblogs.validation.FormValidator的参数
//返回:
//  可链式调用的jQuery对象
$.fn.formValidator = function(options) {
    return this.each(function() {
        var self = $(this);
        if (self.prop('tagName').toLowerCase() == 'form') {
            var validator = self.data('validator');
            if (validator) {
                $.extend(validator, options);
            }
            else {
                self.data('validator', new $.cnblogs.validation.FormValidator(self, options));
            }
        }
    });
};

$.fn.performValidation = function(group, onSuccess, onFailure) {
    this.doValidate = function(validators) {
        var errors = new Array();
        var result = true;

        $.each(validators, function() {
            if (!this.isValid()) {
                result = false;
                if (this.result.msg) {
                    errors.push(this.result.msg);
                }
            }
        });

        //触发事件
        if (result && onSuccess) {
            onSuccess();
        }
        else if (onFailure) {
            onFailure(errors);
        }

        return result;
    };

    var self = $(this);
    if (self.prop('tagName').toLowerCase() == 'form') {
        var validators = self.data('validator').validators;
        if (validators && validators.length > 0) {
            if (group && typeof group == 'string' && group.length > 0) {
                //对此组进行验证
                //获取所有group中的validator
                validators = $.grep(validators, function(validator) {
                    return (validator.group == group);
                });
            }
            //一一验证
            return this.doValidate(validators);
        }
    }

    //默认返回true
    return true;
};

$.cnblogs.validation.InputValidator = $.cnblogs.Extensible.extend({
    //加载时显示的消息
    readyMsg: '',
    //获得焦点后显示的消息
    focusMsg: '',
    //验证通过后显示的消息
    validMsg: '',
    //控件加载时消息的css
    readyClass: 'validation-ready',
    //验证未通过时消息的css
    errorClass: 'validation-error',
    //验证通过时消息的css
    focusClass: 'validation-focus',
    //获得焦点后消息的css
    validClass: 'validation-valid',

    //验证组
    group: null,

    //显示消息的元素的id
    msgTarget: '',

    //待验证的input
    input: null,

    //第一个validator，用以形成责任链
    head: null,
    //最后一个validator，用以形成责任链
    tail: null,

    //添加一个validator，标准的链表操作形式
    addValidator: function(validator) {
        if (validator) {
            if (this.head) {
                this.tail.successor = validator;
                this.tail = validator;
            }
            else {
                this.head = validator;
                this.tail = validator;
            }
        }
    },

    //验证是否通过
    isValid: function() {
        if (!this.result.result) {
            this.validate();
        }
        return this.result.result;
    },

    onError: function(input, msgTarget, cssClass, msg) { },
    onValid: function(input, msgTarget, cssClass, msg) { },
    onReady: function(input, msgTarget, cssClass, msg) { },
    onFocus: function(input, msgTarget, cssClass, msg) { },

    //验证input
    validate: function() {
        //先设验证通过
        this.result.modify(true, this.validMsg, this);

        if (this.head) {
            this.head.validate(this.input, this.result);
        }
    },

    initialize: function(options) {
        options = options || {};

        $.extend(this, options);

        if (this.msgTarget && this.msgTarget != '') {
            this.msgTarget = $('#' + this.msgTarget);
        }

        //如果没有待验证的input元素则返回
        if (!this.input) {
            return;
        }

        //保持this指针的引用
        var self = this;

        //初始化ValidateResult
        this.result = new $.cnblogs.validation.ValidateResult();
        this.result.onModified = function(result) {
            //根据验证结果显示不同的内容
            if (result.result) {
                self.onValid(self.input, self.msgTarget, self.validClass, self.validMsg);
            }
            else {
                self.onError(self.input, self.msgTarget, self.errorClass, result.msg);
            }
        }

        //注册事件
        //这样注册会导致ajax验证的时候连续触发change和blur事件，需要2次请求服务器，很麻烦
        $(this.input).bind('change', function() {
            self.validate();
        }).bind('focus', function() {
            if (!self.isValid()) {
                self.onFocus(self.input, self.msgTarget, self.focusClass, self.focusMsg);
            }
        }).bind('blur', function() {
            if (self.isValid()) {
                self.onValid(self.input, self.msgTarget, self.validClass, self.validMsg);
            }
            else {
                self.onError(self.input, self.msgTarget, self.errorClass, self.result.msg);
            }
        });

        this.onReady(this.input, this.msgTarget, this.readyClass, this.readyMsg);
    }
});

//默认的消息显示方案
$.extend($.cnblogs.validation.InputValidator, {
    //显示消息
    showMsg: function(css, msg) {
        this.getTargetEl().removeClass().addClass(css).html(msg);
    },

    //获取显示消息用的元素
    getTargetEl: function() {
        if (!this.msgTarget || this.msgTarget == '') {
            return $('#' + $(this.input).attr('id') + 'Tip');
        }
        else {
            return this.msgTarget;
        }
    },

    onError: function(input, msgTarget, cssClass, msg) {
        this.showMsg(cssClass, msg);
    },
    onValid: function(input, msgTarget, cssClass, msg) {
        this.showMsg(cssClass, msg);
    },
    onReady: function(input, msgTarget, cssClass, msg) {
        this.showMsg(cssClass, msg);
    },
    onFocus: function(input, msgTarget, cssClass, msg) {
        this.showMsg(cssClass, msg);
    }
});

jQuery.fn.initValidator = function(options) {
    return this.each(function() {
        var form = $(this.form);
        var formValidator = form.data('validator');

        if (!formValidator) {
            //初始化一个空的FormValidator
            form.formValidator({});
        }

        options['input'] = this;
        var validator = new $.cnblogs.validation.InputValidator(options);
        formValidator.addValidator(validator);
        $(this).data('validator', validator);
    });
};

jQuery.fn.addValidator = function(rule, options) {
    return this.each(function() {
        var validator = $(this).data('validator');

        if (validator) {
            validator.addValidator($.cnblogs.validation.ValidatorMgr.createValidator(rule, options));
        }
    });
}

//Required
$.cnblogs.validation.RequiredValidator = $.cnblogs.validation.CustomValidator.extend({
    doValidate: function(input, result) {
        var value = $(input).val();
        if (!value || !value.length > 0) {
            this.setError(result);
        }
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('required', $.cnblogs.validation.RequiredValidator);

//Length
$.cnblogs.validation.LengthValidator = $.cnblogs.validation.CustomValidator.extend({
    min: 0,
    max: 0,

    getMin: function() {
        return this.min || this.min >= 0 ? this.min : 0;
    },

    getMax: function() {
        return this.max || this.max >= 0 ? this.max : 0;
    },

    doValidate: function(input, result) {
        var value = $(input).val();
        value = value.replace(/[^\x00-\xff]/g, 'xx');
        if (!value || value.length < this.getMin() || value.length > this.getMax()) {
            this.setError(result);
        }
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('length', $.cnblogs.validation.LengthValidator);

//Regex
$.cnblogs.validation.RegexValidator = $.cnblogs.validation.CustomValidator.extend({
    acceptTypes: ['text', 'password'],

    regex: '',

    getRegex: function() {
        return new RegExp(this.regex || '');
    },

    doValidate: function(input, result) {
        var regex = this.getRegex();
        if (!regex.test($(input).val())) {
            this.setError(result);
        }
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('regex', $.cnblogs.validation.RegexValidator);

//Value
$.cnblogs.validation.ValueValidator = $.cnblogs.validation.CustomValidator.extend({
    not: null,

    accept: null,

    doValidate: function(input, result) {
        var val = $(input).val();
        if (this.accept && typeof this.accept == 'string' &&
            val != this.accept) {
            this.setError(result);
        }
        else if (this.accept && typeof this.accept == 'object' &&
                this.accept.length && this.accept.length > 0 &&
                $.inArray(val, this.accept) == -1) {
            this.setError(result);
        }
        else if (this.not && typeof this.not == 'string' &&
            val == this.not) {
            this.setError(result);
        }
        else if (this.not && typeof this.not == 'object' &&
            this.not.length && this.not.length > 0 &&
            $.inArray(val, this.not) != -1) {
            this.setError(result);
        }
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('value', $.cnblogs.validation.ValueValidator);

//Ajax
$.cnblogs.validation.AjaxValidator = $.cnblogs.validation.CustomValidator.extend({
    acceptTypes: ['text', 'password'],

    url: '',

    extraParams: {},

    method: 'post',

    username: '',

    password: '',

    contentType: 'application/json; charset=utf-8',

    isPending: false,

    isAsmx: true,

    wrapper: 'd',

    doValidate: function(input, result) {
        if (this.isPending) {
            return;
        }

        var data = { value: $(input).val() };
        $.extend(data, this.extraParams);

        if (this.isAsmx) {
            //如果是调用ASP.NET Web Service,需要将data格式化为json串
            data = JSON.stringify(data);
        }

        var self = this;

        this.isPending = true;

        $.ajax({
            url: this.url,
            type: this.method,
            contentType: this.contentType,
            username: this.username,
            password: this.password,
            cache: false,
            data: data,
            dataType: 'json',

            success: function(data, test) {
                self.isPending = false;
                if (self.wrapper) {
                    data = data[self.wrapper];
                }
                if (data) {
                    if (result.modifier == self) {
                        result.modify(true, '', this);
                    }
                }
                else {
                    self.setError(result);
                }
            },

            error: function(xhr, text) {
                self.isPending = false;
                self.setError(result);
            }
        });

        //服务器返回之前先设为false
        this.setError(result);
    }
});

$.cnblogs.validation.ValidatorMgr.registerRule('ajax', $.cnblogs.validation.AjaxValidator);