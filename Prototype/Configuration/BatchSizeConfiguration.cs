namespace Prototype.Configuration;

public class BatchSizeSettings
{
    public int SmallBatch { get; set; } = 500;    // < 1000 rows
    public int MediumBatch { get; set; } = 1000;  // < 5000 rows  
    public int LargeBatch { get; set; } = 2000;   // < 20000 rows
    public int XLargeBatch { get; set; } = 5000;  // >= 20000 rows
    
    public int GetBatchSize(int rowCount)
    {
        return rowCount switch
        {
            < 1000 => SmallBatch,
            < 5000 => MediumBatch,
            < 20000 => LargeBatch,
            _ => XLargeBatch
        };
    }
}