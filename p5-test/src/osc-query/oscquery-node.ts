import { clamp } from "./oscquery-helpers";

export interface OSCQueryNode {
    'DESCRIPTION': string;
    'FULL_PATH': string;
    'TYPE': typeof OSCQueryTypes.Tags[keyof typeof OSCQueryTypes.Tags];
    'ACCESS'?: typeof OSCQueryAccess[keyof typeof OSCQueryAccess];
    'CONTENTS'?: { [key: string]: OSCQueryNode };
    'VALUE'?: (string | number | boolean | null)[];
    'RANGE'?: OSCRange[];
    
    Listen?: boolean;
    OnValueChanged?: (values: (string | number | boolean | null)[]) => void;
}

export interface OSCRange {
    'MIN'?: number;
    'MAX'?: number;
    'VALS'?: string[];
}

export const OSCQueryAccess = {
    NoValue: 0,
    Read: 1,
    Write: 2,
    ReadWrite: 3,
} as const;


export const OSCQueryTypes = {

    Container: "container",

    Tags: {
        Integer: "i",
        Float: "f",
        String: "s",
        Color: "r",
        Boolean: "T",
    },
} as const;


export interface OSCQueryHostInfo {
    'NAME': string;
    'EXTENSIONS': { [key: string]: boolean };
    'OSC_PORT': number;
    'OSC_TRANSPORT'?: "TCP" | "UDP";
    'METADATA'?: { [key: string]: string };
}


export const createIntNode = (data: {
    path: string,
    name: string,
    value: number
} & Partial<{
    onValueChanged: (value: number) => void,
    min: number
    max: number
}>): OSCQueryNode => {

    const {
        path,
        name,
        value,
        onValueChanged,
        min,
        max,
    } = data;

    return {
        'FULL_PATH': path,
        'DESCRIPTION': name,
        'TYPE': OSCQueryTypes.Tags.Integer,
        'VALUE': [clamp(value, min ?? Number.MIN_SAFE_INTEGER, max ?? Number.MAX_SAFE_INTEGER)],
        'RANGE': (min !== undefined || max !== undefined)
            ? [{ 'MIN': min, 'MAX': max }]
            : undefined,
        OnValueChanged: onValueChanged
            ? (values: (string | number | boolean | null)[]) => {
                if (values.length > 0 && typeof values[0] === 'number') {
                    onValueChanged(values[0]);
                }
            }
          : undefined,
      };
}


export const createFloatNode = (data: {
    path: string,
    name: string,
    value: number
} & Partial<{
    onValueChanged: (value: number) => void,
    min: number
    max: number
}>): OSCQueryNode => {

    const {
        path,
        name,
        value,
        onValueChanged,
        min,
        max,
    } = data;

    return {
        'FULL_PATH': path,
        'DESCRIPTION': name,
        'TYPE': OSCQueryTypes.Tags.Float,
        'VALUE': [clamp(value, min ?? Number.MIN_VALUE, max ?? Number.MAX_VALUE)],
        'RANGE': (min !== undefined || max !== undefined)
            ? [{ 'MIN': min, 'MAX': max }]
            : undefined,
        OnValueChanged: onValueChanged
            ? (values: (string | number | boolean | null)[]) => {
                if (values.length > 0 && typeof values[0] === 'number') {
                    onValueChanged(values[0]);
                }
            }
          : undefined,
      };
}