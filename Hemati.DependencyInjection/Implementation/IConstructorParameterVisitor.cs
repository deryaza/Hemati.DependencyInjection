// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;

namespace Hemati.DependencyInjection.Implementation;

public interface IConstructorParameterVisitor
{
 void VisitParameter(ParameterInfo info, ref FindServiceRequest request);
}