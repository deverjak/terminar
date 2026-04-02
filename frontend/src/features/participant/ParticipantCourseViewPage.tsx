import { useParams, useSearchParams } from 'react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Container, Title, Text, Stack, Card, Badge, Button, Group, Alert, Loader, Center } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { getCourseView, unenroll, excuseFromSession } from './participantApi';

export default function ParticipantCourseViewPage() {
  const { safeLinkToken } = useParams<{ safeLinkToken: string }>();
  const [searchParams] = useSearchParams();
  const tenantSlug = searchParams.get('tenant') ?? localStorage.getItem('tenantSlug') ?? '';
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: view, isLoading, error } = useQuery({
    queryKey: ['participant-course', safeLinkToken],
    queryFn: () => getCourseView(safeLinkToken!, tenantSlug),
    enabled: !!safeLinkToken,
  });

  const unenrollMutation = useMutation({
    mutationFn: () => unenroll(safeLinkToken!, tenantSlug),
    onSuccess: () => {
      notifications.show({ color: 'green', message: t('participant.courseView.unenrollSuccess') });
      queryClient.invalidateQueries({ queryKey: ['participant-course', safeLinkToken] });
    },
    onError: () => notifications.show({ color: 'red', message: 'Failed to unenroll. Please try again.' }),
  });

  const excuseMutation = useMutation({
    mutationFn: (sessionId: string) => excuseFromSession(safeLinkToken!, sessionId, tenantSlug),
    onSuccess: () => {
      notifications.show({ color: 'green', message: 'Excusal submitted.' });
      queryClient.invalidateQueries({ queryKey: ['participant-course', safeLinkToken] });
    },
    onError: () => notifications.show({ color: 'red', message: 'Failed to submit excusal.' }),
  });

  if (isLoading) return <Center mt="xl"><Loader /></Center>;
  if (error || !view) return <Center mt="xl"><Text c="red">Link not found or expired.</Text></Center>;

  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <div>
          <Title order={2}>{view.courseTitle}</Title>
          <Text c="dimmed">{t('participant.courseView.sessions')}</Text>
        </div>

        {view.enrollmentStatus === 'Confirmed' && (
          <Card withBorder>
            {view.canUnenroll ? (
              <Group>
                <Button
                  color="red"
                  variant="outline"
                  loading={unenrollMutation.isPending}
                  onClick={() => { if (confirm(t('participant.courseView.unenrollConfirmBody'))) unenrollMutation.mutate(); }}
                >
                  {t('participant.courseView.unenrollButton')}
                </Button>
                {view.unenrollmentDeadlineAt && (
                  <Text size="sm" c="dimmed">
                    Deadline: {new Date(view.unenrollmentDeadlineAt).toLocaleDateString()}
                  </Text>
                )}
              </Group>
            ) : (
              <Alert color="orange">{t('participant.courseView.unenrollDeadlinePassed')}</Alert>
            )}
          </Card>
        )}

        <Stack gap="sm">
          {view.sessions.length === 0 && <Text c="dimmed">{t('participant.courseView.noSessions')}</Text>}
          {view.sessions.map(session => (
            <Card key={session.sessionId} withBorder>
              <Group justify="space-between" align="flex-start">
                <div>
                  <Text fw={500}>
                    {new Date(session.scheduledAt).toLocaleString()} ({session.durationMinutes} min)
                  </Text>
                  {session.location && <Text size="sm" c="dimmed">{session.location}</Text>}
                </div>
                <div>
                  {session.isPast && <Badge color="gray">Past</Badge>}
                  {session.excusalStatus === 'Excused' && <Badge color="orange">{t('participant.courseView.excusedLabel')}</Badge>}
                  {session.excusalStatus === 'CreditIssued' && <Badge color="blue">Credit issued</Badge>}
                  {!session.isPast && !session.excusalStatus && (
                    session.canExcuse ? (
                      <Button
                        size="xs"
                        variant="outline"
                        loading={excuseMutation.isPending}
                        onClick={() => excuseMutation.mutate(session.sessionId)}
                      >
                        {t('participant.courseView.excuseButton')}
                      </Button>
                    ) : (
                      <Text size="xs" c="dimmed">{t('participant.courseView.deadlinePassed')}</Text>
                    )
                  )}
                </div>
              </Group>
            </Card>
          ))}
        </Stack>
      </Stack>
    </Container>
  );
}
