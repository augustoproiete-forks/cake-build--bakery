﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Cake.Scripting.Reflection.Emitters;
using static Cake.Scripting.CodeGen.Generators.CakeAliasGenerationHelper;

namespace Cake.Scripting.CodeGen.Generators
{
    public sealed class CakeMethodAliasGenerator : ICakeAliasGenerator
    {
        private readonly TypeEmitter _typeEmitter;
        private readonly ParameterEmitter _parameterEmitter;

        public CakeMethodAliasGenerator(TypeEmitter typeEmitter, ParameterEmitter parameterEmitter)
        {
            _typeEmitter = typeEmitter ?? throw new ArgumentNullException(nameof(typeEmitter));
            _parameterEmitter = parameterEmitter ?? throw new ArgumentNullException(nameof(parameterEmitter));
        }

        public void Generate(TextWriter writer, CakeScriptAlias alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Generate(new IndentedTextWriter(writer), alias);
        }

        private void Generate(IndentedTextWriter writer, CakeScriptAlias alias)
        {
            // XML documentation
            WriteDocs(writer, alias.Documentation);

            // Access modifier
            writer.Write("public ");

            // Return type
            if (alias.Method.ReturnType != null)
            {
                if (alias.Method.ReturnType.Namespace.Name == "System" && alias.Method.ReturnType.Name == "Void")
                {
                    writer.Write("void");
                }
                else
                {
                    _typeEmitter.Write(writer, alias.Method.ReturnType);
                }
                writer.Write(" ");
            }

            // Render the method signature.
            writer.Write(alias.Method.Name);

            // Generic arguments?
            if (alias.Method.GenericParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(",", alias.Method.GenericParameters.Select(p => p.Name)));
                writer.Write(">");
            }

            // Arguments
            writer.Write("(");
            WriteMethodParameters(writer, alias, invocation: false);
            writer.Write(")");

            // Generic constraints
            if (alias.Method.GenericParameters.Count > 0)
            {
                foreach (var genericParameter in alias.Method.GenericParameters)
                {
                    if (genericParameter.Constraints.Count > 0)
                    {
                        writer.Write(" where ");
                        writer.Write(genericParameter.Name);
                        writer.Write(" : ");
                        writer.Write(string.Join(",", genericParameter.Constraints));
                    }
                }
            }

            // Block start
            writer.WriteLine();
            writer.Write("{");
            using (writer.BeginScope())
            {
                // Method is obsolete?
                var performInvocation = true;
                if (alias.Obsolete != null)
                {
                    var message = GetObsoleteMessage(alias);

                    if (alias.Obsolete.IsError)
                    {
                        // Error
                        performInvocation = false;
                        writer.Write($"throw new Cake.ScriptServer.CakeException(\"{message}\");");
                    }
                    else
                    {
                        // Warning
                        writer.Write($"Context.Log.Warning(\"Warning: {message}\");");
                    }
                }

                // Render the method invocation?
                if (performInvocation)
                {
                    if (alias.Obsolete != null)
                    {
                        writer.WriteLine();
                        writer.Write("#pragma warning disable 0618");
                        writer.WriteLine();
                    }

                    WriteInvocation(writer, alias);

                    if (alias.Obsolete != null)
                    {
                        writer.WriteLine();
                        writer.Write("#pragma warning restore 0618");
                    }
                }
            }
            writer.Write("}");
        }

        private void WriteInvocation(TextWriter writer, CakeScriptAlias alias)
        {
            // Has return type?
            var hasReturnValue = !(alias.Method.ReturnType.Namespace.Name == "System" && alias.Method.ReturnType.Name == "Void");
            if (hasReturnValue)
            {
                writer.Write("return ");
            }

            // Method name.
            _typeEmitter.Write(writer, alias.Method.DeclaringType);
            writer.Write(".");
            writer.Write(alias.Method.Name);

            // Generic arguments?
            if (alias.Method.GenericParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(",", alias.Method.GenericParameters.Select(p => p.Name)));
                writer.Write(">");
            }

            // Arguments
            writer.Write("(");
            WriteMethodParameters(writer, alias, invocation: true);
            writer.Write(");");
        }

        private void WriteMethodParameters(TextWriter writer, CakeScriptAlias alias, bool invocation)
        {
            var options = ParameterEmitOptions.Default;
            if (invocation)
            {
                options = options | ParameterEmitOptions.Invocation;
            }

            var parameterResult = alias.Method.Parameters
                .Select(p => string.Join(" ", _parameterEmitter.GetString(p, options)))
                .ToList();

            if (parameterResult.Count > 0)
            {
                parameterResult.RemoveAt(0);
                if (invocation)
                {
                    parameterResult.Insert(0, "Context");
                }
                writer.Write(string.Join(", ", parameterResult));
            }
        }
    }
}
