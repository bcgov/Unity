$(function () {

    let suggestionsArray = [];
   

    // Plugin Constructor
    let LinksInput = function (opts) {
        this.options = Object.assign(LinksInput.defaults, opts);
        this.init();
    }
    // Initialize the plugin
    LinksInput.prototype.init = function (opts) {
        this.options = opts ? Object.assign(this.options, opts) : this.options;

        if (this.initialized)
            this.destroy();
        this.orignal_input = document.getElementById(this.options.selector);

        if (!this.orignal_input) {
            console.error("links-input couldn't find an element with the specified ID");
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
    LinksInput.prototype.addLink = function (linkData) {
        let defaultClass = 'tags-common';
        let linkText, linkClass;

        if (typeof linkData === 'string') {
            linkText = linkData;
            linkClass = defaultClass;
           
        } else {
            linkText = linkData.text || '';
            linkClass = linkData.class || defaultClass;
     
        }

        if (this.anyErrors(linkText))
            return;

        this.arr.push(linkText);

        let linkInput = this;

        let link = document.createElement('span');
        link.className = this.options.linkClass + ' ' + linkClass;
        link.innerText = linkText;

        let closeIcon = document.createElement('a');
        closeIcon.innerHTML = '&times;';

        // delete the link when icon is clicked
        closeIcon.addEventListener('click', function (e) {
            e.preventDefault();
            let link = this.parentNode;

            for (let i = 0; i < linkInput.wrapper.childNodes.length; i++) {
                if (linkInput.wrapper.childNodes[i] == link)
                    linkInput.deleteLink(link, i);
            }
        })

        link.appendChild(closeIcon);
        this.wrapper.insertBefore(link, this.input);
        this.orignal_input.value = JSON.stringify(this.arr);

        return this;
    }

    // Delete Tags
    LinksInput.prototype.deleteLink = function (link, i) {
        let self = this;
        link.remove();
            this.arr.splice(i, 1);
            this.orignal_input.value = JSON.stringify(this.arr);
            return this;
    }

    // Make sure input string have no error with the plugin
    LinksInput.prototype.anyErrors = function (string) {
        // if (this.options.max != null && this.arr.length >= this.options.max) {
        //     console.log('max links limit reached');
        //     return true;
        // }

        if (!this.options.duplicate && this.arr.indexOf(string) != -1) {
            console.log('duplicate found " ' + string + ' " ')
            return true;
        }

        return false;
    }

    // Add links programmatically 
    LinksInput.prototype.addData = function (array) {
        let plugin = this;

        array.forEach(function (string) {
            plugin.addLink(string);
        })
        return this;
    }

    // Get the Input String
    LinksInput.prototype.getInputString = function () {
        return this.arr.join(',');
    }
    LinksInput.prototype.setSuggestions = function (sugArray) {
        suggestionsArray = sugArray;
    }


    // destroy the plugin
    LinksInput.prototype.destroy = function () {
        this.orignal_input.removeAttribute('hidden');

        delete this.orignal_input;
        let self = this;

        Object.keys(this).forEach(function (key) {
            if (self[key] instanceof HTMLElement)
                self[key].remove();

            if (key != 'options')
                delete self[key];
        });

        this.initialized = false;
    }

    // Private function to initialize the link input plugin
    function init(links) {
        links.wrapper.append(links.input);
        links.wrapper.classList.add(links.options.wrapperClass);
        links.orignal_input.setAttribute('hidden', 'true');
        links.orignal_input.parentNode.insertBefore(links.wrapper, links.orignal_input);
        links.input.addEventListener('input', function () {
            const inputValue = links.input.value.trim().toLowerCase();

            // Show suggestions only after the first character entry
            if (inputValue.length > 0) {
                const suggestions = suggestionsArray.filter(link => link.toLowerCase().includes(inputValue));

                // Display suggestions below the input element
                if (suggestions.length) {
                    displaySuggestions(links, suggestions);
                } else {
                    removeSuggestions(links);
                }

            } else {
                // Remove suggestions if input is empty
                removeSuggestions(links);
            }
        });
    }

    // Function to display auto-completion suggestions
    function displaySuggestions(links, suggestions) {
        // Remove previous suggestions
        removeSuggestions(links);

        // Create suggestion container
        const suggestionContainer = document.createElement('div');
        suggestionContainer.classList.add('links-suggestion-container');
        const suggestionTitleElement = document.createElement('div');
        suggestionTitleElement.className = 'links-suggestion-title';
        suggestionTitleElement.innerText = 'ALL APPLICATIONS';
        suggestionContainer.appendChild(suggestionTitleElement);

        // Add suggestions to the container
        suggestions.forEach(suggestion => {
            const suggestionElement = document.createElement('div');
            suggestionElement.className = 'links-suggestion-element';
            suggestionElement.innerText = suggestion;

            // Add click event to add suggestion as a new link
            suggestionElement.addEventListener('click', function () {
                links.addLink(suggestion);
                removeSuggestions(links);
                links.input.value = "";
            });

            suggestionContainer.appendChild(suggestionElement);
        });

        // Append the suggestion container below the input
        links.wrapper.appendChild(suggestionContainer);
    }

    // Function to remove auto-completion suggestions
    function removeSuggestions(links) {
        const suggestionContainer = links.wrapper.querySelector('.links-suggestion-container');
        if (suggestionContainer) {
            suggestionContainer.remove();
        }
    }

    // initialize the Events
    function initEvents(links) {
        links.wrapper.addEventListener('click', function () {
            links.input.focus();
        });
    }

    // Set All the Default Values
    LinksInput.defaults = {
        selector: '',
        wrapperClass: 'tags-input-wrapper',
        linkClass: 'tag',
        max: null,
        duplicate: false
    }

    window.LinksInput = LinksInput;


});


