import { Badge } from '@mantine/core';
import { useTranslation } from 'react-i18next';

type BadgeType = 'course' | 'registration' | 'registrationSource' | 'staff' | 'role';

interface StatusBadgeProps {
  type: BadgeType;
  value: string;
}

const courseStatusColors: Record<string, string> = {
  Draft: 'gray',
  Active: 'green',
  Cancelled: 'red',
  Completed: 'blue',
};

const registrationStatusColors: Record<string, string> = {
  Confirmed: 'green',
  Cancelled: 'red',
};

const staffStatusColors: Record<string, string> = {
  Active: 'green',
  Inactive: 'gray',
};

const roleColors: Record<string, string> = {
  Admin: 'blue',
  Staff: 'gray',
};

const registrationSourceColors: Record<string, string> = {
  SelfService: 'teal',
  StaffAdded: 'violet',
};

function getColor(type: BadgeType, value: string): string {
  switch (type) {
    case 'course': return courseStatusColors[value] ?? 'gray';
    case 'registration': return registrationStatusColors[value] ?? 'gray';
    case 'registrationSource': return registrationSourceColors[value] ?? 'gray';
    case 'staff': return staffStatusColors[value] ?? 'gray';
    case 'role': return roleColors[value] ?? 'gray';
  }
}

function getLabelKey(type: BadgeType, value: string): string {
  switch (type) {
    case 'course': return `courses.statuses.${value}`;
    case 'registration': return `registrations.statuses.${value}`;
    case 'registrationSource': return `registrations.sources.${value}`;
    case 'staff': return `staff.statuses.${value}`;
    case 'role': return `staff.roles.${value}`;
  }
}

export function StatusBadge({ type, value }: StatusBadgeProps) {
  const { t } = useTranslation();
  const color = getColor(type, value);
  const label = t(getLabelKey(type, value));

  return <Badge color={color}>{label}</Badge>;
}
