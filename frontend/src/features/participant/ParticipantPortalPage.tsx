import { useEffect, useState } from 'react';
import { useSearchParams, Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import {
  Container,
  Title,
  Text,
  Stack,
  Card,
  Badge,
  Button,
  Group,
  Loader,
  Center,
  Anchor,
} from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { notifications } from '@mantine/notifications';
import { redeemMagicLink, getPortal, type ExcusalCreditSummary } from './participantApi';

const PORTAL_TOKEN_KEY = 'participantPortalToken';
const TENANT_SLUG_KEY = 'tenantSlug';

export default function ParticipantPortalPage() {
  const [searchParams] = useSearchParams();
  const { t } = useTranslation();
  const magicToken = searchParams.get('token');
  const tenantSlug =
    searchParams.get('tenant') ?? localStorage.getItem(TENANT_SLUG_KEY) ?? '';
  const [portalToken, setPortalToken] = useState<string | null>(
    () => localStorage.getItem(PORTAL_TOKEN_KEY)
  );
  const [redeeming, setRedeeming] = useState(false);

  useEffect(() => {
    if (magicToken && !portalToken) {
      setRedeeming(true);
      redeemMagicLink(magicToken)
        .then(({ portalToken: pt }) => {
          localStorage.setItem(PORTAL_TOKEN_KEY, pt);
          setPortalToken(pt);
        })
        .catch(() =>
          notifications.show({ color: 'red', message: 'Link expired or already used.' })
        )
        .finally(() => setRedeeming(false));
    }
  }, [magicToken, portalToken]);

  const { data: portal, isLoading } = useQuery({
    queryKey: ['participant-portal', portalToken],
    queryFn: () => getPortal(portalToken!, tenantSlug),
    enabled: !!portalToken,
  });

  if (redeeming || isLoading)
    return (
      <Center mt="xl">
        <Loader />
      </Center>
    );

  if (!portalToken)
    return (
      <Center mt="xl">
        <Stack align="center">
          <Text>{t('participant.portal.linkExpired')}</Text>
          <Anchor component={Link} to={`/participant?tenant=${tenantSlug}`}>
            {t('participant.portal.requestNewLink')}
          </Anchor>
        </Stack>
      </Center>
    );

  if (!portal)
    return (
      <Center mt="xl">
        <Text c="dimmed">{t('participant.portal.loading')}</Text>
      </Center>
    );

  return (
    <Container size="md" py="xl">
      <Stack gap="xl">
        <Title order={2}>{t('participant.portal.title')}</Title>

        <section>
          <Title order={4} mb="sm">
            {t('participant.portal.enrollmentsSection')}
          </Title>
          {portal.enrollments.length === 0 && (
            <Text c="dimmed">{t('participant.portal.noEnrollments')}</Text>
          )}
          <Stack gap="sm">
            {portal.enrollments.map((e) => (
              <Card key={e.enrollmentId} withBorder>
                <Group justify="space-between">
                  <div>
                    <Text fw={500}>{e.courseTitle}</Text>
                    {e.firstSessionAt && (
                      <Text size="sm" c="dimmed">
                        {t('participant.portal.firstSession')}:{' '}
                        {new Date(e.firstSessionAt).toLocaleDateString()}
                      </Text>
                    )}
                  </div>
                  <Button
                    variant="subtle"
                    component={Link}
                    to={`/participant/course/${e.safeLinkToken}?tenant=${tenantSlug}`}
                  >
                    {t('participant.portal.viewCourse')}
                  </Button>
                </Group>
              </Card>
            ))}
          </Stack>
        </section>

        <section>
          <Title order={4} mb="sm">
            {t('participant.portal.creditsSection')}
          </Title>
          {portal.excusalCredits.length === 0 && (
            <Text c="dimmed">{t('participant.portal.noCredits')}</Text>
          )}
          <Stack gap="sm">
            {portal.excusalCredits.map((credit: ExcusalCreditSummary) => (
              <Card key={credit.creditId} withBorder>
                <Group justify="space-between">
                  <div>
                    <Text fw={500}>{credit.sourceCourseTitle}</Text>
                    <Group gap="xs" mt="xs">
                      {credit.tags.map((tag) => (
                        <Badge key={tag} variant="outline" size="sm">
                          {tag}
                        </Badge>
                      ))}
                    </Group>
                  </div>
                  <Badge color={credit.status === 'Active' ? 'green' : 'gray'}>
                    {t(`participant.excusalCredit.status.${credit.status}`)}
                  </Badge>
                </Group>
              </Card>
            ))}
          </Stack>
        </section>
      </Stack>
    </Container>
  );
}
