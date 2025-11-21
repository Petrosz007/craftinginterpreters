package com.github.petrosz007.craftinginterpreters.slox

import com.github.petrosz007.craftinginterpreters.slox.values.Value
import com.github.petrosz007.craftinginterpreters.slox.utils.OptionExtension.orFailWith

import scala.collection.mutable.ArrayBuffer
import scala.util.{Failure, Success, Try}
import scala.util.control.Exception.catching

enum OpCode(val opCode: Byte):
  case Constant extends OpCode(0)
  case Return   extends OpCode(1)

object OpCode:
  class UnknownOpcode(opCode: Byte) extends RuntimeException(s"Unknown opcode: '$opCode''")

  def fromByte(byte: Byte): Try[OpCode] = byte match
    case 0 => Success(Constant)
    case 1 => Success(Return)
    case x => Failure(new UnknownOpcode(x))

enum Instruction:
  case Simple(opCode: OpCode)
  case Constant(opCode: OpCode, constantOffset: Offset, constant: Value)

  override def toString: String = this match
    case Simple(opCode)                             => opCode.toString
    case Constant(opCode, constantOffset, constant) => f"$opCode%-16s $constantOffset%4d '$constant'"

private type Offset = Int
opaque type Line    = Int
object Line:
  def apply(n: Int): Line = n

class Chunk:
  private val code: ArrayBuffer[Byte]       = ArrayBuffer()
  private val constants: ArrayBuffer[Value] = ArrayBuffer()
  private val lines: ArrayBuffer[Line]      = ArrayBuffer()

  def codeAt(i: Offset): Byte      = code(i)
  def constantAt(i: Offset): Value = constants(i)

  def length: Int = code.length

  def write(byte: Byte, line: Line): Unit =
    code.addOne(byte)
    lines.addOne(line)

  def write(opCode: OpCode, line: Line): Unit = write(opCode.opCode, line)

  def addConstant(constant: Value): Offset =
    constants.addOne(constant)
    constants.length - 1

  def readOpCode(offset: Offset): Try[OpCode] =
    OpCode.fromByte(codeAt(offset))

  def disassemble(name: String): Unit =
    println(s"== $name ==")
    for (offset, instruction) <- ChunkIterator(this) do
      if offset > 0 && lines(offset) == lines(offset - 1)
      then println(f"$offset%04d    | $instruction")
      else println(f"$offset%04d ${lines(offset)}%4d $instruction")

class ChunkIterator(private val chunk: Chunk) extends Iterator[(Offset, Instruction)]:

  import OpCode.*

  private var offset: Offset = 0

  override def hasNext: Boolean = offset < chunk.length

  override def next(): (Offset, Instruction) =
    chunk.readOpCode(offset) match
      case Success(value) =>
        value match
          case x @ Return   => simpleInstruction(x)
          case x @ Constant => constantInstruction(x)
      case Failure(err: UnknownOpcode) =>
        throw new Exception(s"ChunkIterator cannot handle OpCode '${chunk.codeAt(offset)}'")
      case Failure(exception) => throw exception

  private def simpleInstruction(opCode: OpCode): (Offset, Instruction.Simple) =
    val originalOffset = offset
    offset += 1
    (originalOffset, Instruction.Simple(opCode))

  private def constantInstruction(opCode: OpCode): (Offset, Instruction.Constant) =
    val originalOffset = offset
    val constant       = chunk.constantAt(chunk.codeAt(originalOffset + 1)) // TODO: Check for overflow
    offset += 2
    (originalOffset, Instruction.Constant(opCode, originalOffset + 1, constant))
