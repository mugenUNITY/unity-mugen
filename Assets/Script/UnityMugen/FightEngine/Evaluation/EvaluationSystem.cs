﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityMugen.Collections;

namespace UnityMugen.Evaluation
{

    public class EvaluationSystem
    {

        public EvaluationSystem()
        {
            m_expressioncache = new KeyedCollection<string, Expression>(x => x.ToString(), StringComparer.OrdinalIgnoreCase);
            m_tokenizer = new Tokenizer();
            m_treebuilder = new TreeBuilder(this);
            m_compiler2 = new ExpressionCompiler();
            m_compiler = new ILCompiler();
            //Expression exp = null;
            //exp = CreateExpression("0.0 = [-10, 10]");
            //exp = CreateExpression("AnimElem = 2 > 0");
            //exp = CreateExpression("AnimElem = 2, > 0");
            //exp = CreateExpression("AnimElem = 2");
            //var result = exp.Evaluate(null);
        }


        public Expression CreateExpression(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var expression = BuildExpression(input);

            if (expression.IsValid == false)
            {
                UnityEngine.Debug.LogWarning("Error parsing line: " + input);
            }

            return expression;
        }

        public PrefixedExpression CreatePrefixedExpression(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var with = BuildExpression(input);
            var prefix = input[0];
            var without = BuildExpression(input.Substring(1));

            bool? common;
            Expression expression;

            if (prefix == 's' || prefix == 'S')
            {
                if (without.IsValid)
                {
                    common = false;
                    expression = without;
                }
                else
                {
                    common = false;
                    expression = with;
                }
            }
            else if (prefix == 'f' || prefix == 'F')
            {
                if (without.IsValid)
                {
                    common = true;
                    expression = without;
                }
                else
                {
                    common = false;
                    expression = with;
                }
            }
            else
            {
                common = null;
                expression = with;
            }

            if (expression.IsValid == false)
            {
                UnityEngine.Debug.LogWarning("Error parsing line: " + input);
            }

            return new PrefixedExpression(expression, common);
        }

        string teste;
        private Expression BuildExpression(string input)
        {

            if (input == null) throw new ArgumentNullException(nameof(input));


            //Quoted strings must be case sensitive & cache is not case sensitive
            if (input.IndexOf('"') == -1 && m_expressioncache.Contains(input)) return m_expressioncache[input];

            var tokens = m_tokenizer.Tokenize(input);
            var nodes = m_treebuilder.BuildTree(tokens);


            //if (teste != null)
            //    return m_expressioncache[teste];

            teste = input;

            var functions = new List<EvaluationCallback>();
            foreach (var node in nodes)
            {
                var com = m_compiler.Create(node);
                if(com == null)
                    com = m_compiler2.Create(node);
                functions.Add(com);
            }
            var expression = new Expression(input, functions);  // Tiago Souza

            if (input.IndexOf('"') == -1) m_expressioncache.Add(expression);

            return expression;
        }

        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Tokenizer m_tokenizer;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TreeBuilder m_treebuilder;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICompiler m_compiler;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICompiler m_compiler2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private KeyedCollection<string, Expression> m_expressioncache;

        #endregion
    }
}