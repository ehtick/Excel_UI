﻿using BH.oM.Base;
using BH.oM.UI;
using BH.UI.Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using System.Linq.Expressions;
using System.Reflection;
using BH.Engine.Reflection;
using BH.Engine.Reflection.Convert;

namespace BH.UI.Dragon.Templates
{
    public class FormulaDataAccessor : DataAccessor
    {
        /*******************************************/
        /**** Constructors                      ****/
        /*******************************************/

        public FormulaDataAccessor()
        {
        }

        /*******************************************/
        /**** Public Methods                    ****/
        /*******************************************/

        public override T GetDataItem<T>(int index)
        {
            Type type = typeof(T);
            object item = inputs[index];

            if(item is ExcelEmpty || item is ExcelMissing) {
                return (T)defaults[index];
            }
            if (item is object[,])
            {
                // Incase T is object or something similarly cabable of
                // holding a list.
                return (T)(GetDataList<object>(index) as dynamic);
            }
            if (type.IsEnum && item is string)
            {
                return (T)Enum.Parse(type, item as string);
            }

            // Can't always cast directly to T from object storage type even
            // when the actual type as castable to T. So have to use `as
            // dynamic` so the cast is between the actual type of `item` to T.
            return (T)(item as dynamic);
        }

        /*******************************************/

        public override List<T> GetDataList<T>(int index)
        {
            object item = inputs[index];
            if (item is List<T>)
            {
                return item as List<T>;
            }
            if (item is IEnumerable<T>)
            {
                return (item as IEnumerable<T>).ToList();
            }
            if (item is IEnumerable)
            {
                // This will flatten object[,]s
                List<T> list = new List<T>();
                foreach(object o in item as IEnumerable) {
                    if (!(o is ExcelMissing || o is ExcelEmpty))
                    {
                        list.Add((T)(o as dynamic));
                    }
                }
                return list;
            }
            return null;
        }

        /*******************************************/

        public override List<List<T>> GetDataTree<T>(int index)
        {
            object item = inputs[index];
            if (item is List<List<T>>)
            {
                return item as List<List<T>>;
            }
            if (item is object[,])
            {
                // Convert 2D arrays to List<List<T>> with columns as the
                // inner list, e.g.
                //     a1 b1 c1 
                //     a2 b2 c2 
                //     a3 b3 c3 
                //       ->
                //     new List<List<T>>() {
                //         new List<T>() { a1, a2, a3 },
                //         new List<T>() { b1, b2, b3 },
                //         new List<T>() { c1, c2, c3 }
                //     }
                //
                // This is arbitrary, but it has to be one way or the other
                List<List<T>> list = new List<List<T>>();
                int height = (item as object[,]).GetLength(0);
                int width = (item as object[,]).GetLength(1);
                for(int i = 0; i < width; i++)
                {
                    list.Add(new List<T>());
                    for (int j = 0; j < height; j++)
                    {
                        object o = (item as object[,])[j, i];
                        if (!(o is ExcelMissing || o is ExcelEmpty))
                        {
                            list[i].Add((T)(o as dynamic));
                        }
                    }
                }
                return list;
            }
            if (item is IEnumerable)
            {
                return (item as IEnumerable).Cast<object>()
                    .Select(o =>
                        (o is IEnumerable) ? (o as IEnumerable)
                            .Cast<object>()
                            .Select(inner => (T)(inner as dynamic))
                            .ToList()
                            : null as List<T> )
                    .ToList();

            }
            return null;
        }

        /*******************************************/

        public override bool SetDataItem<T>(int index, T data)
        {
            try
            {
                if (data.GetType().IsPrimitive || data is string)
                {
                    output = data;
                    return true;
                }
                if (data is Guid)
                {
                    return SetDataItem(index, data.ToString());
                }
                if(data is IEnumerable && !(data is ICollection) )
                {
                    return SetDataItem(index, (data as IEnumerable).Cast<object>().ToList());
                }
                return SetDataItem(index,
                    data.GetType().ToText() + " [" + Project.ActiveProject.IAdd(data) + "]"
                );
            } catch
            {
                output = ExcelError.ExcelErrorNA;
                return false;
            }
        }

        /*******************************************/

        public override bool SetDataList<T>(int index, IEnumerable<T> data)
        {
            if (data is ICollection)
            {
                return SetDataItem(index, data);
            }
            return SetDataItem(index, data.ToList());
        }

        /*******************************************/

        public override bool SetDataTree<T>(int index,
            IEnumerable<IEnumerable<T>> data)
        {
            if (data is ICollection && data.All(sub => sub is ICollection))
            {
                return SetDataItem(index, data);
            }
            return SetDataItem(index, data.Select(sub=>sub.ToList()).ToList());
        }

        /*******************************************/

        public void StoreDefaults(object [] params_)
        {
            // Collect default values from ParamInfo so defaultable
            // arguments can be ommited in excel
            defaults = params_;
        }

        /*******************************************/

        public void Store(params object[] in_)
        {
            // Store some inputs in this DataAccessor
            // convert Guid strings to objects
            inputs = new object[in_.Length];
            for (int i = 0; i < in_.Length; i++)
            {
                inputs[i] = Evaluate(in_[i]);
            }
        }

        /*******************************************/

        public object GetOutput()
        {
            // Retrieve the output from this DataAccessor
            var errors = Query.CurrentEvents()
                .Where(e => e.Type == oM.Reflection.Debuging.EventType.Error);
            if (errors.Count() > 0) {
                string msg = errors
                    .Select(e => e.Message)
                    .Aggregate((a, b) => a + "\n" + b);
                try
                {
                    ExcelReference caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
                    ExcelAsyncUtil.QueueAsMacro(() => XlCall.Excel(XlCall.xlfNote, msg, caller));
                }
                catch { }
            }
            if (output == null)
            {
                return ExcelError.ExcelErrorNull;
            }
            return output;
        }

        /*******************************************/

        public void ResetOutput()
        {
            try
            {
                ExcelReference caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
                ExcelAsyncUtil.QueueAsMacro(() => XlCall.Excel(XlCall.xlfNote, "", caller));
            }
            catch { }
            output = null;
        }
        
        /*******************************************/
         
        public Tuple<Delegate, ExcelFunctionAttribute, List<object>>
            Wrap(CallerFormula caller, Expression<Action> action)
        {
            // Create a Delegate that looks like:
            //
            // (a, b, c, ...) => {
            //     accessor.ResetOutput();
            //     accessor.StoreDefaults(defaults);
            //     accessor.Store( new [] {a, b, c, ...} );
            //     action();
            //     return accessor.GetOutput();
            // }


            // Create an array of [n] parameters
            var rawParams = caller.Caller.InputParams;
            ParameterExpression[] lambdaParams = rawParams
                .Select(p => Expression.Parameter(typeof(object)))
                .ToArray();
            Expression newArr = Expression.NewArrayInit(
                typeof(object),
                lambdaParams
            );

            Expression defs = Expression.Constant(rawParams.Select(p => p.DefaultValue).ToArray());

            Expression accessorInstance = Expression.Constant(this);
            Type accessorType = GetType();

            // Invoke action
            Expression actionCall = Expression.Invoke(action);

            // Call FormulaDataAccessor.ResetOutput 
            MethodInfo resetMethod = accessorType.GetMethod("ResetOutput");
            Expression resetCall = Expression.Call(
                accessorInstance, // (FormulaDataAccessor)DataAccessor
                resetMethod       // void Reset()
            );

            MethodInfo storeDefMethod = accessorType.GetMethod("StoreDefaults");
            Expression storeDefCall = Expression.Call(
                accessorInstance, // FormulaDataAccessor
                storeDefMethod,   // void StoreDefaults(...)
                defs
            );

            // Call FormulaDataAccessor.Store with array
            MethodInfo storeMethod = accessorType.GetMethod("Store");
            Expression storeCall = Expression.Call(
                accessorInstance, // (FormulaDataAccessor)DataAccessor
                storeMethod,      // void Store(object[])
                newArr            // new [] { ... }
            );
            // Return call FormulaDataAccessor.GetOutput()
            MethodInfo returnMethod = accessorType.GetMethod("GetOutput");
            Expression returnCall = Expression.Call(
                accessorInstance, // (FormulaDataAccessor)DataAccessor
                returnMethod      // object GetOutput()
            );

            // Chain them together
            Expression tree = Expression.Block(
                resetCall,
                storeDefCall,
                storeCall,
                actionCall,
                returnCall
            );
            LambdaExpression lambda = Expression.Lambda(tree, lambdaParams);

            // Compile
            var argAttrs = rawParams
                        .Select(p =>
                        {
                            string desc = p.Description + " " + p.DataType.ToText();
                            if (p.HasDefaultValue)
                            {
                                desc += " [default: " +
                                (p.DefaultValue is string
                                    ? $"\"{p.DefaultValue}\""
                                    : p.DefaultValue == null
                                        ? "null"
                                        : p.DefaultValue.ToString()
                                ) + "]";
                            }

                            return new ExcelArgumentAttribute()
                            {
                                Name = p.HasDefaultValue ? $"[{p.Name}]" : p.Name,
                                Description = desc
                            };
                        }).ToList<object>();
            return new Tuple<Delegate, ExcelFunctionAttribute, List<object>>(
                lambda.Compile(),
                GetFunctionAttribute(caller, rawParams),
                argAttrs
            );
        }

        /*******************************************/
        /**** Private Methods                   ****/
        /*******************************************/

        private object Evaluate(object input)
        {
            if (input.GetType().IsPrimitive)
            {
                return input;
            }
            if(input is string)
            {
                object obj = Project.ActiveProject.GetAny(input as string);
                return obj == null ? input : obj;
            }
            if(input is object[,])
            {
                // Keep the 2D array layout but evaluate members recursively
                // to convert Guid strings into objects from the Project
                return Evaluate(input as object[,]);
            }
            return input;
        }

        /*******************************************/

        private object Evaluate(object[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);

            object[,] evaluated = new object[height, width];
            for(int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    evaluated[j,i] = Evaluate(input[j,i]);
                }
            }
            return evaluated;
        }

        /*******************************************/
        
        private ExcelFunctionAttribute GetFunctionAttribute(CallerFormula caller, IEnumerable<ParamInfo> paramList)
        {
            bool hasParams = paramList.Count() > 0;
            string params_ = "";
            if (hasParams)
            {
                params_ = "?by_" + paramList
                    .Select(p => p.DataType.ToText())
                    .Select(p => p.Replace("[]","s"))
                    .Select(p => p.Replace("[,]","Matrix"))
                    .Select(p => p.Replace("&",""))
                    .Select(p => p.Replace("<","Of"))
                    .Select(p => p.Replace(">",""))
                    .Select(p => p.Replace(", ","_"))
                    .Select(p => p.Replace("`","_"))
                    .Aggregate((a, b) => $"{a}_{b}");
            }

            object info = caller.Caller.SelectedItem;
            string name = caller.Name;
            
            return new ExcelFunctionAttribute()
            {
                Name = name + params_,
                Description = caller.Caller.Description,
                Category = "Dragon." + caller.Caller.Category,
                IsMacroType = true
            };
        }

        /*******************************************/
        /**** Private Fields                    ****/
        /*******************************************/
        
        private object[] inputs;
        private object[] defaults;
        private object output;
    }
}