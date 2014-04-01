using System;
using System.Collections.Generic;
using FluentMigrator.Infrastructure;

namespace FluentMigrator.Model
{
    public enum ConstraintType
    {
        PrimaryKey,
        Unique
    }

    public class ConstraintDefinition : ICloneable, ICanBeConventional, ICanBeValidated, ISupportAdditionalFeatures
    {
        private readonly Dictionary<string, object> _additionalFeatures = new Dictionary<string, object>();

        private ConstraintType constraintType;
        public bool IsPrimaryKeyConstraint { get { return ConstraintType.PrimaryKey == constraintType; } }
        public bool IsUniqueConstraint { get { return ConstraintType.Unique == constraintType; } }
        public bool? IsClustered { get; set; }
        public int? FillFactor { get; set; }

        public virtual string SchemaName { get; set; }
        public virtual string ConstraintName { get; set; }
        public virtual string TableName { get; set; }
        public virtual ICollection<IndexColumnDefinition> Columns { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:ConstraintDefinition"/> class.
        /// </summary>
        public ConstraintDefinition(ConstraintType type)
        {
            constraintType = type;

            Columns = new HashSet<IndexColumnDefinition>();
        }

        #region ICloneable Members

        public object Clone()
        {
            return new ConstraintDefinition(constraintType)
            {
                ConstraintName = ConstraintName,
                SchemaName = SchemaName,
                TableName = TableName,
                Columns = Columns,
                constraintType = constraintType,
                IsClustered = IsClustered,
                FillFactor = FillFactor
            };
        }

        #endregion

        #region ICanBeConventional Members

        public void ApplyConventions(IMigrationConventions conventions)
        {
            if (String.IsNullOrEmpty(ConstraintName)){ 
                ConstraintName = conventions.GetConstraintName(this);
            }
        }

        #endregion

        #region ICanBeValidated Members

        public void CollectValidationErrors(ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(TableName))
            {
                errors.Add(ErrorMessages.TableNameCannotBeNullOrEmpty);
            }

            if (0 == Columns.Count)
            {
                errors.Add(ErrorMessages.ConstraintMustHaveAtLeastOneColumn);
            }
        }

        #endregion


        public IDictionary<string, object> AdditionalFeatures
        {
            get { return _additionalFeatures; }
        }

        void ISupportAdditionalFeatures.AddAdditionalFeature(string feature, object value)
        {
            if (!AdditionalFeatures.ContainsKey(feature))
            {
                AdditionalFeatures.Add(feature, value);
            }
            else
            {
                AdditionalFeatures[feature] = value;
            }
        }
    }
}
