export interface ProgramDetails {
  displayName: string;
  division: string;
  branch: string;
  description: string;
}

// Mirrors ProgramDetailsConsts.cs (Unity.GrantManager.Domain.Shared)
export const PROGRAM_DETAILS_FIELD_LIMITS = {
  displayName: 30,
  division: 50,
  branch: 50,
  description: 500
} as const;
