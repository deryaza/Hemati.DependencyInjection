// SPDX-License-Identifier: LGPL-3.0-only

using System.Diagnostics;
using Hemati.DependencyInjection.Implementation.ServiceDescriptions;

namespace Hemati.DependencyInjection.Implementation;

public class CyclesSupervisor
{
 private readonly List<CyclesSupervisor> _children;
 private readonly HashSet<IServiceDescription> _dependencies;

 public CyclesSupervisor? Parent { get; }

 public IServiceDescription Root { get; }

 public CyclesSupervisor(CyclesSupervisor? parent, IServiceDescription root)
 {
  _children = [];
  _dependencies = [];

  Parent = parent;
  Root = root;
 }

 private bool Check(IServiceDescription childType) => _dependencies.Contains(childType) || _children.Any(x => x.Check(childType));

 public CyclesSupervisor Dive(IServiceDescription dependence)
 {
  Debug.Assert(_dependencies.Contains(dependence));
  CyclesSupervisor child = new(this, dependence);
  _children.Add(child);
  return child;
 }

 public void Remember(IServiceDescription dependency) => _dependencies.Add(dependency);

 public void Forget(IServiceDescription dependency) => _dependencies.Remove(dependency);

 public bool HasCycle()
 {
  bool accumulator = _dependencies.Contains(Root);
  foreach (CyclesSupervisor child in _children)
  {
   accumulator |= child.Check(Root);
  }

  return accumulator;
 }

 public CyclesSupervisor Clone()
 {
  CyclesSupervisor supervisorClone = new(Parent, Root);
  return supervisorClone;
 }

 public void CloneFrom(CyclesSupervisor clone)
 {
  _dependencies.Clear();
  _children.Clear();

  _children.AddRange(clone._children);
  foreach (IServiceDescription cloneDependency in clone._dependencies)
  {
   _dependencies.Add(cloneDependency);
  }
 }
}