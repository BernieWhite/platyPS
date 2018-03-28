using System;
using System.Linq;

namespace Markdown.MAML.Model.MAML
{
    public static class MamlExtensions
    {
        public static bool IsApplicable(this MamlParameter parameter, string[] applicableTag)
        {
            if (applicableTag != null && applicableTag.Length > 0 && parameter.Applicable != null)
            {
                // applicable if intersect is not empty
                return applicableTag.Intersect(parameter.Applicable, StringComparer.OrdinalIgnoreCase).Any();
            }

            // if one is null then it's applicable
            return true;
        }

        public static bool IsMetadataEqual(this MamlParameter parameter, MamlParameter other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(parameter.Name, other.Name) &&
                parameter.Required == other.Required &&
                parameter.Position == other.Position &&
                StringComparer.OrdinalIgnoreCase.Equals(parameter.PipelineInput, other.PipelineInput) &&
                parameter.Globbing == other.Globbing;
        }

        public static bool IsSwitchParameter(this MamlParameter parameter)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(parameter.Type, "SwitchParameter") ||
                StringComparer.OrdinalIgnoreCase.Equals(parameter.Type, "switch");
        }

        public static bool IsNamed(this MamlParameter parameter)
        {
            return !parameter.Position.HasValue;
        }
    }
}
