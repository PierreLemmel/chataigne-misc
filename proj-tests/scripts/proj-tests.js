function log(val) {
    script.log(val);
}

function logWarning(input) {
    script.logWarning(input);
}

function logError(input) {
    script.logError(input);
}


function logProperties(input) {
    var properties = util.getObjectProperties(input);
    log(properties);
}

function logMethods(input) {
    var methods = util.getObjectMethods(input);
    log(methods);
}

