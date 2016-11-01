using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class ForthInterpreter : MonoBehaviour
{

    public InputField inputField;
    public Text outputText;
    public Button runButton;

    private InterpreterState state;
    private Queue<object> inputBuffer;
    private Stack<object> dataStack;
    private Stack<object> compilationStack;
    private Dictionary<string, List<object>> wordDictionary;
    private Dictionary<string, List<object>> wordCompilationDictionary;
    private Primitives primitives;
    private Queue<string> errors;
    private Queue<string> outputBuffer;

    private enum InterpreterState
    {
        INTERPRETING,
        COMPILING
    }

    private enum WordType
    {
        PRIMITIVE,
        USER
    }

    // Use this for initialization
    void Start()
    {
        state = InterpreterState.INTERPRETING;
        inputBuffer = new Queue<object>();
        dataStack = new Stack<object>();
        compilationStack = new Stack<object>();
        wordDictionary = new Dictionary<string, List<object>>();
        wordCompilationDictionary = new Dictionary<string, List<object>>();
        outputBuffer = new Queue<string>();
        errors = new Queue<string>();
        primitives = new Primitives(this);
        primitives.AddToDictionary();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ParseInputField();
        }
        if (inputBuffer.Count > 0)
        {
            InterpretInputBuffer();
        }
        if (outputBuffer.Count > 0)
        {
            FlushOutputBuffer();
        }
    }

    public void ParseInputField()
    {
        string input = inputField.text.Trim();
        if (input.Length > 0)
        {
            ParseIntoInputBuffer(input);
        }
        else
        {
            lock (outputBuffer)
            {
                outputBuffer.Enqueue("ok");
            }
        }
    }

    private void ParseIntoInputBuffer(string input)
    {
        List<string> strings = new List<string>(input.Split(' '));
        lock (inputBuffer)
        {
            foreach (string s in strings)
            {
                inputBuffer.Enqueue(s);
            }
        }
    }

    private void InterpretInputBuffer()
    {
        Queue<object> tokens = new Queue<object>();
        lock (inputBuffer)
        {
            while (inputBuffer.Count > 0)
            {
                tokens.Enqueue(inputBuffer.Dequeue());
            }
        }
        ProcessTokens(tokens);
        Queue<string> errs = new Queue<string>();
        lock (errors)
        {
            while (errors.Count > 0)
            {
                errs.Enqueue(errors.Dequeue());
            }
        }
        lock (outputBuffer)
        {
            while (errs.Count > 0)
            {
                outputBuffer.Enqueue(errs.Dequeue());
                outputBuffer.Enqueue("\n");
            }
            if (outputBuffer.Count == 0)
            {
                outputBuffer.Enqueue("ok");
            }
        }
    }

    private void ProcessTokens(Queue<object> tokens)
    {
        while (tokens.Count > 0)
        {
            ProcessToken(tokens.Dequeue());
        }
    }

    private void FlushOutputBuffer()
    {
        Queue<object> output = new Queue<object>();
        lock (outputBuffer)
        {
            while (outputBuffer.Count > 0)
            {
                object o = outputBuffer.Dequeue();
                output.Enqueue(o);
            }
        }
        string s = "";
        while (output.Count > 0)
        {
            s += output.Dequeue().ToString();
        }
        outputText.text = s;
    }

    private void ProcessToken(object token)
    {
        switch (state)
        {
            case InterpreterState.INTERPRETING:
                ProcessTokenInterpreting(token);
                break;
            case InterpreterState.COMPILING:
                ProcessTokenCompiling(token);
                break;
        }
    }

    private void ProcessTokenInterpreting(object token)
    {
        if (token is string)
        {
            ProcessStringToken(token as string);
        }
        else
        {
            ProcessObjectToken(token);
        }
    }

    private void ProcessTokenCompiling(object token)
    {
        if (token is string)
        {
            ProcessStringTokenCompiling(token as string);
        }
        else
        {
            ProcessObjectTokenCompiling(token);
        }
    }

    private void ProcessStringTokenCompiling(string token)
    {
        if (wordDictionary.ContainsKey(token) && wordCompilationDictionary.ContainsKey(token))
        {
            ProcessCompilationDictionaryToken(token);
        }
        else
        {
            compilationStack.Push(token);
        }
    }

    private void ProcessObjectTokenCompiling(object token)
    {
        compilationStack.Push(token);
    }

    private void ProcessStringToken(string token)
    {
        float number;
        if (wordDictionary.ContainsKey(token))
        {
            ProcessExistingDictionaryToken(token);
        }
        else if (float.TryParse(token, out number))
        {
            dataStack.Push(number);
        }
        else
        {
            errors.Enqueue("Undefined word: " + token);
        }
    }

    private void ProcessExistingDictionaryToken(string token)
    {
        List<object> code;
        if (wordDictionary.TryGetValue(token, out code))
        {
            WordType? wType = code[0] as WordType?;
            if (wType != null)
            {
                switch (wType.Value)
                {
                    case WordType.PRIMITIVE:
                        primitives.ExecuteWord(token);
                        break;
                    case WordType.USER:
                        Queue<object> tokens = new Queue<object>(code.GetRange(1, code.Count - 1));
                        ProcessTokens(tokens);
                        break;
                }
            }
            else
            {
                errors.Enqueue("null word type for word=" + token);
            }
        }
    }

    private void ProcessCompilationDictionaryToken(string token)
    {
        List<object> code;
        if (wordCompilationDictionary.TryGetValue(token, out code))
        {
            WordType? wType = code[0] as WordType?;
            if (wType != null)
            {
                switch (wType.Value)
                {
                    case WordType.PRIMITIVE:
                        primitives.ExecuteCompilationWord(token);
                        break;
                    case WordType.USER:
                        errors.Enqueue("unsupported compliation word type user for word=" + token);
                        break;
                }
            }
            else
            {
                errors.Enqueue("null ocmpilation word type for word=" + token);
            }
        }
    }

    private void ProcessObjectToken(object token)
    {
        dataStack.Push(token);
    }

    private class Primitives
    {
        private ForthInterpreter forth;
        private delegate void PrimitiveFunc();
        private Dictionary<string, PrimitiveFunc> primitiveMap;
        private Dictionary<string, PrimitiveFunc> primitiveCompilationMap;

        public Primitives(ForthInterpreter instance)
        {
            forth = instance;
        }

        public void ExecuteWord(string word)
        {
            PrimitiveFunc func;
            if (primitiveMap.TryGetValue(word, out func))
            {
                func();
            }
            else
            {
                forth.errors.Enqueue("no function found for primitive word=" + word);
            }
        }

        public void ExecuteCompilationWord(string word)
        {
            PrimitiveFunc func;
            if (primitiveCompilationMap.TryGetValue(word, out func))
            {
                func();
            }
            else
            {
                forth.errors.Enqueue("no function found for primitive compilation word=" + word);
            }
        }

        public void AddToDictionary()
        {
            primitiveMap = new Dictionary<string, PrimitiveFunc>()
            {
                { ":", colon },
                { ";", semicolon },
                { "*", _multiply },
                { "+", _plus },
                { ".s", _show },
                { "clearstacks", clearstacks },
                { "dup", dup },
                { "words", words }
            };
            primitiveCompilationMap = new Dictionary<string, PrimitiveFunc>()
            {
                { ";", semicolon_compilation }
            };
            foreach (string word in primitiveMap.Keys)
            {
                forth.wordDictionary.Add(word, new List<object>() { WordType.PRIMITIVE });
            }
            foreach (string word in primitiveCompilationMap.Keys)
            {
                forth.wordCompilationDictionary.Add(word, new List<object>() { WordType.PRIMITIVE });
            }
        }

        #region primitives

        private void colon()
        {
            try
            {
                forth.state = InterpreterState.COMPILING;
                // def will be ended after semicolon
            }
            catch (Exception e)
            {
                forth.errors.Enqueue(":(colon) needs a string and then commands ended with a semicolon, e=" + e);
            }
        }

        private void semicolon()
        {
            forth.errors.Enqueue(";(semicolon) cannot be called in interpreting mode");
        }

        private void semicolon_compilation()
        {
            try
            {
                List<object> code = new List<object>() { WordType.USER };
                while (forth.compilationStack.Count > 1)
                {
                    object o = forth.compilationStack.Pop();
                    code.Add(o);
                }
                string word = forth.compilationStack.Pop() as string;
                forth.wordDictionary.Add(word, code);
                forth.state = InterpreterState.INTERPRETING;
            }
            catch (Exception e)
            {
                forth.errors.Enqueue(";(semicolon) couldn't add word definition, e=" + e);
            }
        }

        private void _multiply()
        {
            try
            {
                float? a = forth.dataStack.Pop() as float?;
                float? b = forth.dataStack.Pop() as float?;
                if (a.HasValue && b.HasValue)
                {
                    float c = a.Value * b.Value;
                    forth.dataStack.Push(c);
                }
                else
                {
                    throw new Exception("only two floats are supported");
                }
            }
            catch (Exception e)
            {
                forth.errors.Enqueue("*(multiply) needs two numbers on the stack, e=" + e);
            }
        }

        private void _plus()
        {
            try
            {
                float? a = forth.dataStack.Pop() as float?;
                float? b = forth.dataStack.Pop() as float?;
                if (a.HasValue && b.HasValue)
                {
                    float c = a.Value + b.Value;
                    forth.dataStack.Push(c);
                }
                else
                {
                    throw new Exception("only two floats are supported");
                }
            }
            catch (Exception e)
            {
                forth.errors.Enqueue("+(plus) needs two numbers on the stack, e=" + e);
            }
        }

        private void _show()
        {
            Stack<object> stack = new Stack<object>(forth.dataStack);
            forth.outputBuffer.Enqueue(string.Format("<{0}>", stack.Count));
            while (stack.Count > 0)
            {
                string s = stack.Pop().ToString();
                forth.outputBuffer.Enqueue(" ");
                forth.outputBuffer.Enqueue(s);
            }
        }

        private void clearstacks()
        {
            forth.dataStack.Clear();
        }

        private void dup()
        {
            if (forth.dataStack.Count > 0)
            {
                forth.dataStack.Push(forth.dataStack.Peek());
            }
            else
            {
                forth.errors.Enqueue("dup not allowed on empty stack");
            }
        }

        private void words()
        {
            int count = forth.wordDictionary.Keys.Count;
            forth.outputBuffer.Enqueue("<" + count.ToString() + ">");
            IEnumerable<string> words = forth.wordDictionary.Keys;
            foreach (string word in words)
            {
                forth.outputBuffer.Enqueue(" ");
                forth.outputBuffer.Enqueue(word);
            }
        }

        #endregion primitives
    }
}
