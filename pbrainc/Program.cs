using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace ParksComputing.Pbrain;

/// <summary>
/// Compiler implements the pbrain compiler.
/// </summary>
class Compiler {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args) {
        if (args.Length > 0) {
            String fileName = args[0];

            Compiler compiler;
            compiler = new Compiler(fileName);

            Type myType = compiler.Compile();
        }
    }

    private String fileName;
    private String asmName;
    private String asmFileName;

    private AssemblyBuilder myAsmBldr;

    private FieldBuilder mem;
    private FieldBuilder mp;
    private FieldBuilder tmp;
    private FieldBuilder vtbl;

    private TypeBuilder myTypeBldr;

    private MethodInfo readMI;
    private MethodInfo writeMI;
    private MethodInfo hashAddMI;
    private MethodInfo hashGetMI;

    private int methodCount;
    private int callCount;


    void Ldc(ILGenerator il, int count) {
        switch (count) {
            case 0:
                il.Emit(OpCodes.Ldc_I4_0);
                break;

            case 1:
                il.Emit(OpCodes.Ldc_I4_1);
                break;

            case 2:
                il.Emit(OpCodes.Ldc_I4_2);
                break;

            case 3:
                il.Emit(OpCodes.Ldc_I4_3);
                break;

            case 4:
                il.Emit(OpCodes.Ldc_I4_4);
                break;

            case 5:
                il.Emit(OpCodes.Ldc_I4_5);
                break;

            case 6:
                il.Emit(OpCodes.Ldc_I4_6);
                break;

            case 7:
                il.Emit(OpCodes.Ldc_I4_7);
                break;

            case 8:
                il.Emit(OpCodes.Ldc_I4_8);
                break;

            default:
                il.Emit(OpCodes.Ldc_I4, count);
                break;
        }
    }


    void Forward(ILGenerator il, int count) {
        il.Emit(OpCodes.Ldsfld, mp);
        Ldc(il, count);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stsfld, mp);
    }


    void Back(ILGenerator il, int count) {
        il.Emit(OpCodes.Ldsfld, mp);
        Ldc(il, count);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Stsfld, mp);
    }


    void Plus(ILGenerator il, int count) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        Ldc(il, count);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stsfld, tmp);
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldsfld, tmp);
        il.Emit(OpCodes.Stelem_I4);
    }


    void Minus(ILGenerator il, int count) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        Ldc(il, count);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Stsfld, tmp);
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldsfld, tmp);
        il.Emit(OpCodes.Stelem_I4);
    }


    void Read(ILGenerator il) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.EmitCall(OpCodes.Call, readMI, null);
        il.Emit(OpCodes.Stelem_I4);
    }


    void Write(ILGenerator il) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        il.EmitCall(OpCodes.Call, writeMI, null);
    }


    void LoopBegin(ILGenerator il, Label endLabel) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        il.Emit(OpCodes.Brfalse, endLabel);
    }


    void LoopEnd(ILGenerator il, Label beginLabel) {
        il.Emit(OpCodes.Br, beginLabel);
    }


    void Call(ILGenerator il) {
        il.Emit(OpCodes.Ldsfld, vtbl);
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        il.Emit(OpCodes.Box, typeof(int));
        il.EmitCall(OpCodes.Call, hashGetMI, null);
        il.EmitCalli(OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, null, null);
    }


    void Zero(ILGenerator il) {
        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stelem_I4);
    }


    Type Compile() {
        mp = myTypeBldr.DefineField("mp", typeof(int), FieldAttributes.Private | FieldAttributes.Static);
        mem = myTypeBldr.DefineField("mem", typeof(int[]), FieldAttributes.Private | FieldAttributes.Static);
        tmp = myTypeBldr.DefineField("tmp", typeof(int), FieldAttributes.Private | FieldAttributes.Static);

        MethodBuilder mainBldr = myTypeBldr.DefineMethod(
           "main",
           (MethodAttributes)(MethodAttributes.Private | MethodAttributes.Static),
           typeof(int),
           null
           );

        ILGenerator il = mainBldr.GetILGenerator();

        il.Emit(OpCodes.Ldc_I4, 30000);
        il.Emit(OpCodes.Newarr, typeof(int));
        il.Emit(OpCodes.Stsfld, mem);

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stsfld, mp);

        Parse(il);

        il.Emit(OpCodes.Ldsfld, mem);
        il.Emit(OpCodes.Ldsfld, mp);
        il.Emit(OpCodes.Ldelem_I4);
        il.Emit(OpCodes.Ret);

        Type pboutType = myTypeBldr.CreateType();
        myAsmBldr.SetEntryPoint(mainBldr);
        myAsmBldr.Save(asmFileName);
        Console.WriteLine("Assembly saved as '{0}'.", asmFileName);

        return pboutType;
    }


    void Parse(ILGenerator il) {
        using (FileStream fs = File.OpenRead(fileName)) {
            char c;
            int n;

            Queue q = new Queue();

            while ((n = fs.ReadByte()) != -1) {
                c = (char)n;
                q.Enqueue(c);

                if (c == ':') {
                    ++callCount;
                }
            }

            if (callCount > 0) {
                vtbl = myTypeBldr.DefineField("vtbl", typeof(Object), FieldAttributes.Private | FieldAttributes.Static);

                Type hashtableType = typeof(System.Collections.Hashtable);
                ConstructorInfo constructorInfo = hashtableType.GetConstructor(
                   BindingFlags.Instance | BindingFlags.Public,
                   null,
                   CallingConventions.HasThis,
                   Type.EmptyTypes,
                   null
                   );
                il.Emit(OpCodes.Newobj, constructorInfo);
                il.Emit(OpCodes.Stsfld, vtbl);
            }

            Interpret(q, il);
        }
    }


    MethodBuilder Procedure(Queue q) {
        StringBuilder sb = new StringBuilder();
        sb.Append("pb_");
        sb.Append(methodCount);
        String name = sb.ToString();

        MethodBuilder procBldr = myTypeBldr.DefineMethod(
           name,
           (MethodAttributes.Private | MethodAttributes.Static),
           null,
           Type.EmptyTypes
           );

        ILGenerator il = procBldr.GetILGenerator();

        Interpret(q, il);

        il.Emit(OpCodes.Ret);

        return procBldr;
    }


    int CountDuplicates(Queue q, char c) {
        int count = 1;
        char inst = c;

        while (c == inst && q.Count > 0) {
            c = (char)q.Peek();

            if (c == inst) {
                c = (char)q.Dequeue();
                ++count;
            }
        }

        return count;
    }


    void Interpret(Queue q, ILGenerator il) {
        System.Collections.IEnumerator myEnumerator = q.GetEnumerator();

        char c;
        byte b;

        while (q.Count > 0) {
            c = (char)q.Dequeue();

            switch (c) {
                case '+':
                    Plus(il, CountDuplicates(q, c));
                    break;

                case '-':
                    Minus(il, CountDuplicates(q, c));
                    break;

                case '>':
                    Forward(il, CountDuplicates(q, c));
                    break;

                case '<':
                    Back(il, CountDuplicates(q, c));
                    break;

                case ',':
                    Read(il);
                    break;

                case '.':
                    Write(il);
                    break;

                case '[': {
                        if (q.Count > 0) {
                            Queue lq = new Queue();

                            int nest = 0;
                            int startPos = q.Count;
                            bool pair = false;
                            bool zero = false;
                            bool opt = true;

                            while (q.Count > 0) {
                                c = (char)q.Dequeue();

                                if (c == '[') {
                                    ++nest;
                                }
                                else if (c == ']') {
                                    if (nest > 0) {
                                        --nest;
                                    }
                                    else {
                                        pair = true;
                                        break;
                                    }
                                }
                                else if (opt && c == '-' && (startPos - q.Count) == 1) {
                                    opt = false;

                                    if ((char)q.Peek() == ']') {
                                        c = (char)q.Dequeue();
                                        zero = true;
                                        break;
                                    }
                                }

                                lq.Enqueue(c);
                            }

                            if (zero) {
                                Zero(il);
                                break;
                            }

                            if (q.Count != 0 && !pair) {
                                throw new Exception("Unmatched [ found");
                            }

                            Label beginLabel = il.DefineLabel();
                            Label endLabel = il.DefineLabel();

                            il.MarkLabel(beginLabel);
                            LoopBegin(il, endLabel);

                            Interpret(lq, il);
                            LoopEnd(il, beginLabel);
                            il.MarkLabel(endLabel);
                        }
                    }
                    break;

                case '(': {
                        if (q.Count > 0) {
                            bool pair = false;

                            Queue lq = new Queue();

                            int nest = 0;

                            while (q.Count > 0) {
                                c = (char)q.Dequeue();

                                if (c == '(') {
                                    ++nest;
                                }
                                else if (c == ')') {
                                    if (nest > 0) {
                                        --nest;
                                    }
                                    else {
                                        pair = true;
                                        break;
                                    }
                                }

                                lq.Enqueue(c);
                            }

                            if (q.Count != 0 && !pair) {
                                throw new Exception("Unmatched ( found");
                            }

                            MethodBuilder procBldr = Procedure(lq);

                            il.Emit(OpCodes.Ldsfld, vtbl);
                            il.Emit(OpCodes.Ldsfld, mem);
                            il.Emit(OpCodes.Ldsfld, mp);
                            il.Emit(OpCodes.Ldelem_I4);
                            il.Emit(OpCodes.Box, typeof(int));
                            il.Emit(OpCodes.Ldftn, procBldr);
                            il.EmitCall(OpCodes.Call, hashAddMI, null);
                        }

                        ++methodCount;
                    }
                    break;

                case ':':
                    Call(il);
                    break;

                default:
                    break;
            }
        }
    }


    Compiler(String fileNameInit) {
        fileName = fileNameInit;
        methodCount = 0;
        callCount = 0;
        asmName = Path.GetFileNameWithoutExtension(fileName);
        asmFileName = Path.GetFileName(Path.ChangeExtension(fileName, ".exe"));

        AssemblyName myAsmName = new AssemblyName();
        myAsmName.Name = asmName;

        myAsmBldr = AppDomain.CurrentDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);

        Type[] temp1 = { typeof(Char) };
        writeMI = typeof(Console).GetMethod("Write", temp1);
        readMI = typeof(Console).GetMethod("Read");

        Type[] temp2 = { typeof(Object), typeof(Object) };
        hashAddMI = typeof(Hashtable).GetMethod("Add", temp2);

        Type[] temp3 = { typeof(Object) };
        hashGetMI = typeof(Hashtable).GetMethod("get_Item", temp3);

        ModuleBuilder myModuleBldr = myAsmBldr.DefineDynamicModule(asmFileName, asmFileName);
        myTypeBldr = myModuleBldr.DefineType(asmName);
    }
};
