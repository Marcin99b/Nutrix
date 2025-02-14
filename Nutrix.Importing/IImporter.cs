using Nutrix.Commons.ETL;
using Nutrix.Database.Procedures;

namespace Nutrix.Importing;
public interface IImporter
{
    AddOrUpdateProductInput Import(ImportRequest request);
}