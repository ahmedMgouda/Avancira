export {};

declare global {
  namespace google.accounts.id {
    interface CredentialResponse {
      credential: string;
    }

    interface IdConfiguration {
      client_id: string;
      callback: (response: CredentialResponse) => void;
      ux_mode?: 'popup' | 'redirect';
    }

    interface PromptMomentNotification {
      isNotDisplayed(): boolean;
      isSkippedMoment(): boolean;
      isDismissedMoment(): boolean;
      getNotDisplayedReason(): string;
    }

    type PromptCallback = (notification: PromptMomentNotification) => void;
  }

  var google: {
    accounts: {
      id: {
        initialize(config: google.accounts.id.IdConfiguration): void;
        prompt(callback?: google.accounts.id.PromptCallback): void;
      };
    };
  };
}
