package com.github.petrosz007.craftinginterpreters.slox

import com.github.petrosz007.craftinginterpreters.slox.OpCode.{Constant, Return}
import com.github.petrosz007.craftinginterpreters.slox.values.Value

import scala.compiletime.{constValue, error, summonAll}

object Main:
  def main(args: Array[String]): Unit =
    val chunk = Chunk()

    val constant = chunk.addConstant(Value(12.0))
    chunk.write(Constant, Line(123))
    chunk.write(constant.toByte, Line(123))

    chunk.write(Return, Line(124))

    chunk.disassemble("test")

    VM.interpret(chunk)
