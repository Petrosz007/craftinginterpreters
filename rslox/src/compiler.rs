use crate::{
    chunk::{Chunk, OpCode},
    disassembler::disassemble_chunk,
    scanner::{Scanner, Token, TokenType},
};

struct Parser<'a> {
    scanner: Scanner<'a>,
    current: Token,
    previous: Token,
    had_error: bool,
    in_panic_mode: bool,
}

impl<'a> Parser<'a> {
    fn new(scanner: Scanner) -> Parser {
        Parser {
            scanner,
            // TODO: Replace these two initialisations with a more Rust native way
            current: Token {
                typ: TokenType::Error,
                str: "Parser not started yet".to_owned(),
                line: 0,
            },
            previous: Token {
                typ: TokenType::Error,
                str: "Parser not started yet".to_owned(),
                line: 0,
            },
            had_error: false,
            in_panic_mode: false,
        }
    }

    fn consume(&mut self, token_typ: TokenType, error_message_on_fail: &str) {
        if self.current.typ == token_typ {
            self.advance();
            return;
        }

        self.error_at_current(error_message_on_fail); // TODO: Handle this in a more rust native way (this function should return an error)
    }

    fn advance(&mut self) {
        self.previous = self.current.clone();

        loop {
            self.current = self.scanner.scan_token();
            if self.current.typ != TokenType::Error {
                break;
            }

            self.error_at_current(&self.current.str.clone());
        }
    }

    fn error(&mut self, message: &str) {
        self.error_at(&self.previous.clone(), message)
    }

    fn error_at_current(&mut self, message: &str) {
        self.error_at(&self.current.clone(), message)
    }

    fn error_at(&mut self, token: &Token, message: &str) {
        if self.in_panic_mode {
            return;
        }
        self.in_panic_mode = true;

        eprint!("[line {}] Error", token.line);

        match token.typ {
            TokenType::Eof => eprint!(" at end"),
            TokenType::Error => {}
            _ => eprint!(" at '{}'", token.str),
        }

        eprintln!(": {message}");

        self.had_error = true;
    }
}

#[derive(PartialEq, Eq, PartialOrd, Ord, Debug, Copy, Clone)]
enum Precedence {
    Non,
    Assignment, // =
    Or,         // or
    And,        // and
    Equality,   // == !=
    Comparison, // < > <= >=
    Term,       // + -
    Factor,     // * /
    Unary,      // ! -
    Call,       // . ()
    Primary,
}

impl Precedence {
    const fn next_higher(&self) -> Precedence {
        match *self {
            Precedence::Non => Precedence::Assignment,
            Precedence::Assignment => Precedence::Or,
            Precedence::Or => Precedence::And,
            Precedence::And => Precedence::Equality,
            Precedence::Equality => Precedence::Comparison,
            Precedence::Comparison => Precedence::Term,
            Precedence::Term => Precedence::Factor,
            Precedence::Factor => Precedence::Unary,
            Precedence::Unary => Precedence::Call,
            Precedence::Call => Precedence::Primary,
            Precedence::Primary => Precedence::Primary, // There is no higher precedence than Primary
        }
    }
}

type ParseFn<'a> = fn(&mut Compiler<'a>);

struct ParseRule<'a> {
    prefix: Option<ParseFn<'a>>,
    infix: Option<ParseFn<'a>>,
    precedence: Precedence,
}

const fn get_rule<'a>(token_type: TokenType) -> ParseRule<'a> {
    use Precedence::*;

    #[rustfmt::skip]
    let parse_rule = match token_type {
        TokenType::LeftParen    => ParseRule { prefix: Some(Compiler::grouping),infix: None,                    precedence: Non      },
        TokenType::RightParen   => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::LeftBrace    => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::RightBrace   => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Comma        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Dot          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Minus        => ParseRule { prefix: Some(Compiler::unary),   infix: Some(Compiler::binary),  precedence: Term     },
        TokenType::Plus         => ParseRule { prefix: None,                    infix: Some(Compiler::binary),  precedence: Term     },
        TokenType::Semicolon    => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Slash        => ParseRule { prefix: None,                    infix: Some(Compiler::binary),  precedence: Factor   },
        TokenType::Star         => ParseRule { prefix: None,                    infix: Some(Compiler::binary),  precedence: Factor   },
        TokenType::Bang         => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::BangEqual    => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Equal        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::EqualEqual   => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Greater      => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::GreaterEqual => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Less         => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::LessEqual    => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Identifier   => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::String       => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Number       => ParseRule { prefix: Some(Compiler::number),  infix: None,                    precedence: Non      },
        TokenType::And          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Class        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Else         => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::False        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::For          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Fun          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::If           => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Nil          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Or           => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Print        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Return       => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Super        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::This         => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::True         => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Var          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::While        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Error        => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
        TokenType::Eof          => ParseRule { prefix: None,                    infix: None,                    precedence: Non      },
    };

    parse_rule
}

/// Variadic byte emission
macro_rules! emit_bytes {
    ($compiler:ident, $($byte:expr),+) => {{
        $($compiler.emit_byte($byte);)+
    }};
}

pub struct Compiler<'a> {
    current_chunk: Chunk,
    parser: Parser<'a>,
}

impl<'a> Compiler<'a> {
    fn new(parser: Parser) -> Compiler {
        Compiler {
            current_chunk: Chunk::new(),
            parser,
        }
    }

    pub fn compile(source: &str) -> Result<Chunk, ()> {
        let scanner = Scanner::new(source);
        let parser = Parser::new(scanner);
        let mut compiler = Compiler::new(parser);

        compiler.parser.advance();
        compiler.expression();
        compiler
            .parser
            .consume(TokenType::Eof, "Expected end of expression.");

        compiler.emit_return();

        if compiler.parser.had_error {
            Err(())
        } else {
            #[cfg(feature = "debug_print_code")]
            {
                disassemble_chunk(&compiler.current_chunk, "code");
            }
            Ok(compiler.current_chunk)
        }
    }

    //------Parsing------
    fn expression(&mut self) {
        self.parse_precedence(Precedence::Assignment);
    }

    fn number(&mut self) {
        let value = self
            .parser
            .previous
            .str
            .parse::<f64>()
            .expect("scanned number token to be f64");
        self.emit_constant(value);
    }

    fn grouping(&mut self) {
        self.expression();
        self.parser
            .consume(TokenType::RightParen, "Expect a ')' after expression");
    }

    fn unary(&mut self) {
        let operator_type = self.parser.previous.typ;

        // Compile the operand
        self.parse_precedence(Precedence::Unary);

        // Emit the operator instruction
        match operator_type {
            TokenType::Minus => emit_bytes!(self, OpCode::Negate.into()),
            op => unreachable!("Illegal unary operator: `{op:?}`"),
        }
    }

    fn binary(&mut self) {
        let operator_type = self.parser.previous.typ;
        let rule = get_rule(operator_type);
        self.parse_precedence(rule.precedence.next_higher());

        match operator_type {
            TokenType::Plus => emit_bytes!(self, OpCode::Add.into()),
            TokenType::Minus => emit_bytes!(self, OpCode::Subtract.into()),
            TokenType::Star => emit_bytes!(self, OpCode::Multiply.into()),
            TokenType::Slash => emit_bytes!(self, OpCode::Divide.into()),
            op => unreachable!("Illegal binary operator: `{op:?}`"),
        }
    }

    fn parse_precedence(&mut self, precedence: Precedence) {
        self.parser.advance();
        let Some(prefix_rule) = get_rule(self.parser.previous.typ).prefix else {
            self.parser.error("Expect expression.");
            return;
        };

        prefix_rule(self);

        while precedence <= get_rule(self.parser.current.typ).precedence {
            self.parser.advance();
            let infix_rule = get_rule(self.parser.previous.typ)
                .infix
                .expect("infix rule to exist for infix operation");
            infix_rule(self);
        }
    }

    //------Emission------
    fn emit_byte(&mut self, byte: u8) {
        self.current_chunk.write(byte, self.parser.previous.line);
    }

    fn emit_return(&mut self) {
        emit_bytes!(self, OpCode::Return.into());
    }

    fn emit_constant(&mut self, value: f64) {
        self.current_chunk
            .write_constant(value, self.parser.previous.line);
    }
}
