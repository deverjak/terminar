import { useState } from 'react';
import { useSearchParams } from 'react-router';
import { Container, Title, Text, TextInput, Button, Stack, Alert } from '@mantine/core';
import { useTranslation } from 'react-i18next';
import { requestMagicLink } from './participantApi';

export default function ParticipantPortalRequestPage() {
  const [searchParams] = useSearchParams();
  const tenantSlug = searchParams.get('tenant') ?? localStorage.getItem('tenantSlug') ?? '';
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await requestMagicLink(email, tenantSlug);
      setSubmitted(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container size="sm" py="xl">
      <Stack gap="lg">
        <Title order={2}>{t('participant.requestPage.title')}</Title>
        <Text c="dimmed">{t('participant.requestPage.subtitle')}</Text>
        {submitted ? (
          <Alert color="green" title={t('participant.requestPage.successTitle')}>
            {t('participant.requestPage.successMessage')}
          </Alert>
        ) : (
          <form onSubmit={handleSubmit}>
            <Stack gap="md">
              <TextInput
                label={t('participant.requestPage.emailLabel')}
                placeholder={t('participant.requestPage.emailPlaceholder')}
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
              />
              <Button type="submit" loading={loading}>
                {t('participant.requestPage.submitButton')}
              </Button>
            </Stack>
          </form>
        )}
      </Stack>
    </Container>
  );
}
