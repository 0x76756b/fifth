class TokenType{
    constructor(name, regex) {
        this.name = name;
        this.regex = regex;
    }
}

class Token{
    constructor(type, value, position, line = 1, column = 1, is_nullable = false) {
        this.type = type;
        this.value = value;
        this.position = position;
        this.line = line;
        this.column = column;
        this.is_nullable = is_nullable;
    }
}

const token_types = {
    NUMBER: new TokenType("NUMBER", /^[0-9]+/),
    STRING: new TokenType("STRING", /^"([^"\\]|\\.)*"/),
    IDENTIFIER: new TokenType("IDENTIFIER", /^[a-zA-Z_][a-zA-Z0-9_]*/),
    OPERATOR: new TokenType("OPERATOR", /^[+\-*\/=<>!]+/),
}