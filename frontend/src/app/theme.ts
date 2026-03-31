import { createTheme, localStorageColorSchemeManager } from '@mantine/core';

export const theme = createTheme({
  primaryColor: 'blue',
  fontFamily: 'system-ui, sans-serif',
});

export const colorSchemeManager = localStorageColorSchemeManager({ key: 'terminar-color-scheme' });
