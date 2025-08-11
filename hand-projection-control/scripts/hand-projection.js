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


function savePositions() {

    var left0 = root.customVariables.handsControl.variables.leftPosition.leftPosition.get();
    var right0 = root.customVariables.handsControl.variables.rightPosition.rightPosition.get();

    root.customVariables.handsControl.variables.left0.left0.set(left0);
    root.customVariables.handsControl.variables.right0.right0.set(right0);


    var leftPosOutput0 = root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.get();
    var rightPosOutput0 = root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.get();


    if ((leftPosOutput0[0] > rightPosOutput0[0]) !== (left0[0] > right0[0])) {

        var temp = leftPosOutput0[0];
        leftPosOutput0[0] = rightPosOutput0[0];
        rightPosOutput0[0] = temp;
    }

    if ((leftPosOutput0[1] > rightPosOutput0[1]) !== (left0[1] > right0[1])) {

        var temp = leftPosOutput0[1];
        leftPosOutput0[1] = rightPosOutput0[1];
        rightPosOutput0[1] = temp;
    }


    root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.set(leftPosOutput0);
    root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.set(rightPosOutput0);

    root.customVariables.handsControl.variables.leftOutput0.leftOutput0.set(leftPosOutput0);
    root.customVariables.handsControl.variables.rightOutput0.rightOutput0.set(rightPosOutput0);

    var dx = leftPosOutput0[0] - rightPosOutput0[0];
    var dy = leftPosOutput0[1] - rightPosOutput0[1];
    
    var d = Math.sqrt(dx * dx + dy * dy) / 1.414;

    var minScaleFactor = root.customVariables.handsControlSettings.variables.minScaleFactor.minScaleFactor.get();

    var scaleFactor = clamp(d, minScaleFactor, 1);
    root.customVariables.handsControl.variables.pinchingScaleFactor.pinchingScaleFactor.set(scaleFactor);
}

function updatePinchingPositions(leftPos, rightPos) {

    var left0 = root.customVariables.handsControl.variables.left0.left0.get();
    var right0 = root.customVariables.handsControl.variables.right0.right0.get();

    var leftOut0 = root.customVariables.handsControl.variables.leftOutput0.leftOutput0.get();
    var rightOut0 = root.customVariables.handsControl.variables.rightOutput0.rightOutput0.get();

    var a = root.customVariables.handsModifiable.variables.pinchVelocity.pinchVelocity.get()
        * root.customVariables.handsControl.variables.pinchingScaleFactor.pinchingScaleFactor.get();

    var leftOut = [
        clamp01(leftOut0[0] + a * (leftPos[0] - left0[0])),
        clamp01(leftOut0[1] + a * (leftPos[1] - left0[1]))
    ];

    var rightOut = [
        clamp01(rightOut0[0] + a * (rightPos[0] - right0[0])),
        clamp01(rightOut0[1] + a * (rightPos[1] - right0[1]))
    ];

    root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.set(leftOut);
    root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.set(rightOut);
}

function clamp(val, min, max) {
    return Math.max(min, Math.min(max, val));
}

function clamp01(val) {
    return clamp(val, 0, 1);
}

function setPositionsOnDoubleGrab() {

    var leftPos = root.customVariables.handsControl.variables.leftPosition.leftPosition.get();
    var rightPos = root.customVariables.handsControl.variables.rightPosition.rightPosition.get();

    var leftOut = root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.get();
    var rightOut = root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.get();

    if ((leftPos[0] > rightPos[0]) !== (leftOut[0] > rightOut[0])) {

        var temp = leftPos[0];
        leftPos[0] = rightPos[0];
        rightPos[0] = temp;
    }

    if ((leftPos[1] > rightPos[1]) !== (leftOut[1] > rightOut[1])) {

        var temp = leftPos[1];
        leftPos[1] = rightPos[1];
        rightPos[1] = temp;
    }


    root.customVariables.handsControl.variables.doubleGrabTargetLeft.doubleGrabTargetLeft.set(leftPos);
    root.customVariables.handsControl.variables.doubleGrabTargetRight.doubleGrabTargetRight.set(rightPos);
}

function setPositionsOnLeftGrab(leftPos) {
    var left0 = root.customVariables.handsControl.variables.left0.left0.get();


    var rightOut0 = root.customVariables.handsControl.variables.rightOutput0.rightOutput0.get();
    var leftOut0 = root.customVariables.handsControl.variables.leftOutput0.leftOutput0.get();


    var a = root.customVariables.handsModifiable.variables.oneGrabbingVelocity.oneGrabbingVelocity.get()
        * root.customVariables.handsControl.variables.pinchingScaleFactor.pinchingScaleFactor.get();

    var delta = [
        a * (leftPos[0] - left0[0]),
        a * (leftPos[1] - left0[1])
    ];

    var leftOut = [
        clamp01(leftOut0[0] + delta[0]),
        clamp01(leftOut0[1] + delta[1])
    ];

    var rightOut = [
        clamp01(rightOut0[0] + delta[0]),
        clamp01(rightOut0[1] + delta[1])
    ];

    root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.set(leftOut);
    root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.set(rightOut);
}

function setPositionsOnRightGrab(rightPos) {
    var right0 = root.customVariables.handsControl.variables.right0.right0.get();

    var leftOut0 = root.customVariables.handsControl.variables.leftOutput0.leftOutput0.get();
    var rightOut0 = root.customVariables.handsControl.variables.rightOutput0.rightOutput0.get();

    var a = root.customVariables.handsModifiable.variables.oneGrabbingVelocity.oneGrabbingVelocity.get()
        * root.customVariables.handsControl.variables.pinchingScaleFactor.pinchingScaleFactor.get();


    var delta = [
        a * (rightPos[0] - right0[0]),
        a * (rightPos[1] - right0[1])
    ];

    var leftOut = [
        clamp01(leftOut0[0] + delta[0]),
        clamp01(leftOut0[1] + delta[1])
    ];

    var rightOut = [
        clamp01(rightOut0[0] + delta[0]),
        clamp01(rightOut0[1] + delta[1])
    ];

    root.customVariables.handsControl.variables.leftPosition_Output.leftPosition_Output.set(leftOut);
    root.customVariables.handsControl.variables.rightPosition_Output.rightPosition_Output.set(rightOut);
}