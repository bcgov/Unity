using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentScoresDto:IValidatableObject
    {
        public Guid AssessmentId { get; set; }
        public long? FinancialAnalysis { get; set; }
        public long? EconomicImpact { get; set; }
        public long? InclusiveGrowth { get; set; }
        public long? CleanGrowth { get; set; }

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            if((FinancialAnalysis != null && FinancialAnalysis > int.MaxValue) ||
                (EconomicImpact!=null && EconomicImpact>int.MaxValue) ||
                (InclusiveGrowth!=null && InclusiveGrowth>int.MaxValue) ||
                (CleanGrowth!=null && CleanGrowth>int.MaxValue))
            {
                yield return new ValidationResult(
                    "Invalid input!  Scores have maximum value of "+int.MaxValue+".",
                    null
                );
            }
        }
    }
}
