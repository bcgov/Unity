$(function () {

  

    // Function to generate random ID
    function generateRandomId() {
        return Math.random().toString(36).substr(2, 10);
    }

    // Plugin Constructor
    var TagsInput = function (opts) {
        this.options = Object.assign(TagsInput.defaults, opts);
        this.init();
    }
    // Initialize the plugin
    TagsInput.prototype.init = function (opts) {
        this.options = opts ? Object.assign(this.options, opts) : this.options;

        if (this.initialized)
            this.destroy();

        if (!(this.orignal_input = document.getElementById(this.options.selector))) {
            console.error("tags-input couldn't find an element with the specified ID");
            return this;
        }

        this.arr = [];
        this.wrapper = document.createElement('div');
        this.input = document.createElement('input');
        init(this);
        initEvents(this);

        this.initialized = true;
        return this;
    }


    // Add Tags
    TagsInput.prototype.addTag = function (tagData) {
        var defaultClass = 'primary';
        var tagText, tagClass, tagId;

        if (typeof tagData === 'string') {
            tagText = tagData;
            tagClass = defaultClass;
            tagId = generateRandomId();
        } else {
            tagText = tagData.text || '';
            tagClass = tagData.class || defaultClass;
            tagId = tagData.id || generateRandomId();
        }

        if (this.anyErrors(tagText))
            return;

        this.arr.push(tagText);

        var tagInput = this;

        var tag = document.createElement('span');
        tag.className = this.options.tagClass + ' ' + tagClass;
        tag.innerText = tagText;

        var closeIcon = document.createElement('a');
        closeIcon.innerHTML = '&times;';

        // delete the tag when icon is clicked
        closeIcon.addEventListener('click', function (e) {
            e.preventDefault();
            var tag = this.parentNode;

            for (var i = 0; i < tagInput.wrapper.childNodes.length; i++) {
                if (tagInput.wrapper.childNodes[i] == tag)
                    tagInput.deleteTag(tag, i);
            }
        })

        tag.appendChild(closeIcon);
        this.wrapper.insertBefore(tag, this.input);
        this.orignal_input.value = JSON.stringify(this.arr);

        return this;
    }

    // Delete Tags
    TagsInput.prototype.deleteTag = function (tag, i) {
        tag.remove();
        this.arr.splice(i, 1);
        this.orignal_input.value = this.arr.join(',');
        return this;
    }

    // Make sure input string have no error with the plugin
    TagsInput.prototype.anyErrors = function (string) {
        if (this.options.max != null && this.arr.length >= this.options.max) {
            console.log('max tags limit reached');
            return true;
        }

        if (!this.options.duplicate && this.arr.indexOf(string) != -1) {
            console.log('duplicate found " ' + string + ' " ')
            return true;
        }

        return false;
    }

    // Add tags programmatically 
    TagsInput.prototype.addData = function (array) {
        var plugin = this;

        array.forEach(function (string) {
            plugin.addTag(string);
        })
        return this;
    }

    // Get the Input String
    TagsInput.prototype.getInputString = function () {
        return this.arr.join(',');
    }


    // destroy the plugin
    TagsInput.prototype.destroy = function () {
        this.orignal_input.removeAttribute('hidden');

        delete this.orignal_input;
        var self = this;

        Object.keys(this).forEach(function (key) {
            if (self[key] instanceof HTMLElement)
                self[key].remove();

            if (key != 'options')
                delete self[key];
        });

        this.initialized = false;
    }

    // Private function to initialize the tag input plugin
    function init(tags) {
        tags.wrapper.append(tags.input);
        tags.wrapper.classList.add(tags.options.wrapperClass);
        tags.orignal_input.setAttribute('hidden', 'true');
        tags.orignal_input.parentNode.insertBefore(tags.wrapper, tags.orignal_input);
    }

    // initialize the Events
    function initEvents(tags) {
        tags.wrapper.addEventListener('click', function () {
            tags.input.focus();
        });


        tags.input.addEventListener('keydown', function (e) {
            var str = tags.input.value.trim();

            if (!!(~[9, 13, 188].indexOf(e.keyCode))) {
                e.preventDefault();
                tags.input.value = "";
                if (str != "")
                    tags.addTag(str);
            }

        });
    }
    TagsInput.prototype.getTags = function () {
        return this.arr.slice(); // Return a copy of the array to prevent external modification
    }

    // Set All the Default Values
    TagsInput.defaults = {
        selector: '',
        wrapperClass: 'tags-input-wrapper',
        tagClass: 'tag',
        max: null,
        duplicate: false
    }

    window.TagsInput = TagsInput;
    let tagInput1 = new TagsInput({
        selector: 'SelectedTags',
        duplicate: false,
        max: 10
    });

    let uncommonTags = $('#UncommonTags').val();
    let commonTags = $('#CommonTags').val();

    console.log(uncommonTags)
    const uncommonTagsArray = uncommonTags.split(',');
    const commonTagsArray = commonTags.split(',');
    let tagInputArray = [];
    if (commonTagsArray.length) {
        tagInputArray.push({ text: 'CommonTags', class: 'primary', id:0 })
    }
    uncommonTagsArray.forEach(function (item,index) {
        tagInputArray.push({ text: item, class: 'primary', id: index +1 })
    });

    tagInput1.addData(tagInputArray);

    console.log(document.getElementById('SelectedTags').value);

});

// Usage
