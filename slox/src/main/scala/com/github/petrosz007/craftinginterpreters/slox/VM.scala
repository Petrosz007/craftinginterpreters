package com.github.petrosz007.craftinginterpreters.slox

import com.github.petrosz007.craftinginterpreters.slox.utils.Continue
import com.github.petrosz007.craftinginterpreters.slox.values.Value

import scala.util.{boundary, Failure, Success, Try}
import scala.util.boundary.break

enum InterpretResult:
  case Ok, CompileError, RuntimeError

class VM:
  private var chunk: Chunk = Chunk()
  private var ip: Offset   = 0

  private def this(chunk: Chunk) =
    this()
    this.chunk = chunk

  inline def readByte(): Byte =
    val byte = chunk.codeAt(ip)
    ip += 1
    byte

  inline def readOpCode(): Try[OpCode] =
    for
      opCode <- chunk.readOpCode(ip)
      _ = ip += 1
    yield opCode

  inline def readConstant(): Try[Value] =
    Success(chunk.constantAt(readByte()))

  private def run(): InterpretResult =
    import OpCode.*

    while true do
      val instructionResult = readOpCode() match
        case Success(value) =>
          value match
            case Return => return InterpretResult.Ok
            case Constant =>
              for
                constant <- readConstant()
                _ = println(constant)
              yield ()
        case x @ Failure(exception) => x


    InterpretResult.Ok

object VM:
  def interpret(chunk: Chunk): InterpretResult =
    val vm = VM(chunk)
    vm.run()
