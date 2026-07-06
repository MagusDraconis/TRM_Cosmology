namespace TRM.Core.Lattice;

public interface ILatticeService
{
    LatticeSnapshot ComputeSnapshot(LatticeInput input);
}
